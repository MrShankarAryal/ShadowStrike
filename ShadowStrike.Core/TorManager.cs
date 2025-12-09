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
        public static int TorPort { get; private set; } = 9050;
        public static bool IsRunning { get; private set; } = false;

        public static async Task<bool> StartTorAsync()
        {
            // 1. Check if already running
            if (await CheckTorConnectionAsync(9150))
            {
                TorPort = 9150;
                IsRunning = true;
                return true;
            }
            if (await CheckTorConnectionAsync(9050))
            {
                TorPort = 9050;
                IsRunning = true;
                return true;
            }

            // 2. Start Tor Process
            if (!File.Exists(_torPath))
            {
                return false;
            }

            try
            {
                _torProcess = new Process();
                _torProcess.StartInfo.FileName = _torPath;
                // Enable ControlPort 9051 for IP Rotation
                _torProcess.StartInfo.Arguments = "--SocksPort 9050 --ControlPort 9051"; 
                _torProcess.StartInfo.UseShellExecute = false;
                _torProcess.StartInfo.CreateNoWindow = true;
                _torProcess.StartInfo.RedirectStandardOutput = true;
                _torProcess.Start();

                await Task.Delay(5000); 

                if (await CheckTorConnectionAsync(9050))
                {
                    TorPort = 9050;
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

        public static async Task RotateIdentityAsync()
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", 9051);
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
                    await reader.ReadLineAsync(); // Read response
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
