using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public static class TorManager
    {
        private static Process? _torProcess;
        private static string _torPath = @"C:\Users\Shankar Aryal\OneDrive\Desktop\Tor Browser\Browser\TorBrowser\Tor\tor.exe";
        private static int _controlPort = 9051;
        public static int TorPort { get; private set; } = 9050;
        public static bool IsRunning { get; private set; } = false;

        public static async Task<bool> StartTorAsync(ShadowStrike.Core.Anonymity.AnonymitySettings? settings = null)
        {
            settings ??= ShadowStrike.Core.Anonymity.AnonymitySettings.Load();
            int targetPort = settings?.TorSocksPort ?? 9050;
            int controlPort = settings?.TorControlPort ?? 9051;
            _controlPort = controlPort;

            // 1. Check if already running
            if (await CheckTorConnectionAsync(targetPort))
            {
                TorPort = targetPort;
                IsRunning = true;
                return true;
            }
            if (targetPort != 9150 && await CheckTorConnectionAsync(9150))
            {
                TorPort = 9150;
                IsRunning = true;
                return true;
            }
            if (targetPort != 9050 && await CheckTorConnectionAsync(9050))
            {
                TorPort = 9050;
                IsRunning = true;
                return true;
            }

            // 2. Path Discovery
            string? discoveredPath = null;

            // a. Settings path
            if (settings != null && !string.IsNullOrWhiteSpace(settings.TorExecutablePath) && File.Exists(settings.TorExecutablePath))
            {
                discoveredPath = settings.TorExecutablePath;
            }

            // b. PATH environment variable
            if (discoveredPath == null)
            {
                var pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathEnv))
                {
                    var paths = pathEnv.Split(Path.PathSeparator);
                    foreach (var path in paths)
                    {
                        var fullPath = Path.Combine(path, "tor.exe");
                        if (File.Exists(fullPath))
                        {
                            discoveredPath = fullPath;
                            break;
                        }
                    }
                }
            }

            // c. %LOCALAPPDATA%\Tor Browser\Browser\TorBrowser\Tor\tor.exe
            if (discoveredPath == null)
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var torBrowserPath = Path.Combine(localAppData, @"Tor Browser\Browser\TorBrowser\Tor\tor.exe");
                if (File.Exists(torBrowserPath))
                {
                    discoveredPath = torBrowserPath;
                }
            }

            // d. C:\Tor\tor.exe
            if (discoveredPath == null && File.Exists(@"C:\Tor\tor.exe"))
            {
                discoveredPath = @"C:\Tor\tor.exe";
            }

            // e. C:\Program Files\Tor\tor.exe
            if (discoveredPath == null && File.Exists(@"C:\Program Files\Tor\tor.exe"))
            {
                discoveredPath = @"C:\Program Files\Tor\tor.exe";
            }

            // f. {InstallDir}\Tor\tor.exe  — bundled with installer (Option A)
            if (discoveredPath == null)
            {
                var bundledPath = Path.Combine(AppContext.BaseDirectory, "Tor", "tor.exe");
                if (File.Exists(bundledPath))
                {
                    discoveredPath = bundledPath;
                }
            }

            // g. {LocalAppData}\ShadowStrike\Tor\tor.exe — auto-downloaded fallback
            if (discoveredPath == null)
            {
                var downloadedPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ShadowStrike", "Tor", "tor.exe");
                if (File.Exists(downloadedPath))
                {
                    discoveredPath = downloadedPath;
                }
            }

            // Fallback to the original hardcoded path if none found
            if (discoveredPath == null)
            {
                discoveredPath = _torPath;
            }

            if (!File.Exists(discoveredPath))
            {
                return false;
            }

            try
            {
                RotationCount = 0;
                _rotationCount = 0;
                _torProcess = new Process();
                _torProcess.StartInfo.FileName = discoveredPath;
                // Enable ControlPort dynamically
                _torProcess.StartInfo.Arguments = $"--SocksPort {targetPort} --ControlPort {controlPort}"; 
                _torProcess.StartInfo.UseShellExecute = false;
                _torProcess.StartInfo.CreateNoWindow = true;
                _torProcess.StartInfo.RedirectStandardOutput = true;
                _torProcess.Start();

                await Task.Delay(5000); 

                if (await CheckTorConnectionAsync(targetPort))
                {
                    TorPort = targetPort;
                    IsRunning = true;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        // ── IP Rotation ──────────────────────────────────────────────────────────

        /// <summary>Number of successful Tor circuit rotations since last start.</summary>
        public static int RotationCount { get; private set; } = 0;
        private static int _rotationCount = 0;

        public static async Task RotateIdentityAsync()
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", _controlPort);
                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream) { AutoFlush = true };
                using var reader = new StreamReader(stream);

                // Authenticate (assuming no password for our integrated instance)
                await writer.WriteLineAsync("AUTHENTICATE \"\"");
                var response = await reader.ReadLineAsync();
                
                if (response != null && response.StartsWith("250"))
                {
                    // Send NEWNYM signal
                    await writer.WriteLineAsync("SIGNAL NEWNYM");
                    var newnymResponse = await reader.ReadLineAsync();
                    if (newnymResponse != null && newnymResponse.StartsWith("250"))
                    {
                        // Increment rotation counter only on confirmed success
                        RotationCount = Interlocked.Increment(ref _rotationCount);
                    }
                }
            }
            catch 
            {
                // Ignore errors (e.g. if using External Tor without control port access)
            }
        }

        public static void StopTor()
        {
            try
            {
                if (_torProcess != null && !_torProcess.HasExited)
                {
                    _torProcess.Kill();
                    _torProcess = null;
                }
            }
            catch { }
            IsRunning = false;
        }

        private static CancellationTokenSource? _rotationCts;

        public static void StartRotationService(int intervalSeconds = 7)
        {
            if (_rotationCts != null) return; // Already running

            _rotationCts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_rotationCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), _rotationCts.Token);
                        if (IsRunning)
                        {
                            await RotateIdentityAsync();
                        }
                    }
                    catch (TaskCanceledException) { break; }
                    catch { }
                }
            }, _rotationCts.Token);
        }

        public static void StopRotationService()
        {
            _rotationCts?.Cancel();
            _rotationCts = null;
        }

        // ── Auto-Download (Option A fallback for portable / no-installer use) ────

        /// <summary>
        /// Downloads the official Tor Expert Bundle for Windows (x86_64) and extracts
        /// tor.exe + required DLLs to %LocalAppData%\ShadowStrike\Tor\.
        /// Called when no Tor installation is found anywhere on the system.
        /// </summary>
        public static async Task<bool> DownloadTorAsync(IProgress<string>? progress = null)
        {
            const string TorBundleUrl =
                "https://archive.torproject.org/tor-package-archive/torbrowser/14.5.3/tor-expert-bundle-windows-x86_64-14.5.3.tar.gz";

            var destDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ShadowStrike", "Tor");

            try
            {
                Directory.CreateDirectory(destDir);

                var tmpTar = Path.Combine(Path.GetTempPath(), "shadowstrike-tor-bundle.tar.gz");

                // Download
                progress?.Report("Downloading Tor Expert Bundle (~7 MB)…");
                using (var http = new System.Net.Http.HttpClient())
                {
                    http.Timeout = TimeSpan.FromMinutes(3);
                    var data = await http.GetByteArrayAsync(TorBundleUrl);
                    await File.WriteAllBytesAsync(tmpTar, data);
                }

                // Extract with system tar (built into Windows 10+ / Server 2019+)
                progress?.Report("Extracting Tor binaries…");
                var tarArgs = $"-xzf \"{tmpTar}\" -C \"{destDir}\" --strip-components=1 tor/tor.exe tor/libssl-3.dll tor/libcrypto-3.dll tor/zlib1.dll";
                var psi = new ProcessStartInfo("tar", tarArgs)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                };
                var proc = Process.Start(psi)!;
                await proc.WaitForExitAsync();

                try { File.Delete(tmpTar); } catch { }

                if (File.Exists(Path.Combine(destDir, "tor.exe")))
                {
                    progress?.Report("Tor downloaded successfully ✓");
                    return true;
                }

                progress?.Report("Download completed but tor.exe not found in archive.");
                return false;
            }
            catch (Exception ex)
            {
                progress?.Report($"Tor download failed: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> CheckTorConnectionAsync(int port)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync("127.0.0.1", port);
                if (await Task.WhenAny(connectTask, Task.Delay(1000)) == connectTask)
                {
                    return client.Connected;
                }
            }
            catch { }
            return false;
        }
    }
}
