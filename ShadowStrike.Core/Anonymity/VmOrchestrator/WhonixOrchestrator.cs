using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowStrike.Core.Anonymity.VmOrchestrator
{
    public interface IVmOrchestrator
    {
        Task<bool> DetectVirtualBoxAsync();
        Task EnsureImagesPresentAsync(IProgress<AnonymityProgress>? progress = null, CancellationToken ct = default);
        Task ImportImagesAsync(IProgress<AnonymityProgress>? progress = null, CancellationToken ct = default);
        Task ConfigureNetworksAsync();
        Task StartGatewayAsync();
        Task StartWorkstationAsync();
        Task StopAllAsync();
        Task SnapshotAsync(string name = "clean");
        Task RestoreSnapshotAsync(string name = "clean");
    }

    public class WhonixOrchestrator : IVmOrchestrator
    {
        private readonly AnonymitySettings _settings;

        public WhonixOrchestrator(AnonymitySettings? settings = null)
        {
            _settings = settings ?? AnonymitySettings.Load();
        }

        public async Task<bool> DetectVirtualBoxAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var output = RunVBoxManage("--version");
                    return !string.IsNullOrWhiteSpace(output);
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task EnsureImagesPresentAsync(IProgress<AnonymityProgress>? progress = null, CancellationToken ct = default)
        {
            // Check if VMs already imported
            var list = RunVBoxManage("list vms");
            bool gatewayImported = list.Contains(_settings.WhonixGatewayVmName);
            bool workstationImported = list.Contains(_settings.WhonixWorkstationVmName);

            if (gatewayImported && workstationImported)
            {
                progress?.Report(new AnonymityProgress(1, 4, "Whonix VMs already imported."));
                return;
            }

            // Check for user-supplied OVA or download
            Directory.CreateDirectory(_settings.WhonixDownloadCacheDir);
            var gwOva = _settings.WhonixGatewayOvaPath ?? Path.Combine(_settings.WhonixDownloadCacheDir, "Whonix-Gateway.ova");
            var wsOva = _settings.WhonixWorkstationOvaPath ?? Path.Combine(_settings.WhonixDownloadCacheDir, "Whonix-Workstation.ova");

            if (!File.Exists(gwOva))
            {
                await DownloadAndVerifyOvaAsync($"{_settings.WhonixMirrorUrl}/Whonix-Gateway.ova", gwOva, progress, ct);
            }
            if (!File.Exists(wsOva))
            {
                await DownloadAndVerifyOvaAsync($"{_settings.WhonixMirrorUrl}/Whonix-Workstation.ova", wsOva, progress, ct);
            }

            _settings.WhonixGatewayOvaPath = gwOva;
            _settings.WhonixWorkstationOvaPath = wsOva;
            _settings.Save();
        }

        private async Task DownloadAndVerifyOvaAsync(string url, string destination, IProgress<AnonymityProgress>? progress, CancellationToken ct)
        {
            progress?.Report(new AnonymityProgress(1, 4, $"Downloading Whonix OVA from {url}..."));
            using var client = new HttpClient { Timeout = TimeSpan.FromHours(2) };
            
            // Download file
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
                await using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                await contentStream.CopyToAsync(fileStream, ct);
            }

            // Download SHA-512 sum file if available
            try
            {
                var shaUrl = url + ".sha512sum";
                var shaStr = await client.GetStringAsync(shaUrl, ct);
                var expectedSha = shaStr.Split(' ', '\t')[0].Trim();

                using var sha512 = SHA512.Create();
                await using var fileStream = File.OpenRead(destination);
                var hashBytes = await sha512.ComputeHashAsync(fileStream, ct);
                var actualSha = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                if (!string.Equals(expectedSha, actualSha, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(destination);
                    throw new InvalidDataException($"SHA-512 verification failed for {destination}! Download compromised or corrupt.");
                }
            }
            catch (Exception ex) when (ex is not InvalidDataException)
            {
                // Warn if checksum file missing from mirror
                progress?.Report(new AnonymityProgress(1, 4, $"Warning: Could not fetch SHA-512 sum file ({ex.Message}). Continuing with downloaded image."));
            }
        }

        public async Task ImportImagesAsync(IProgress<AnonymityProgress>? progress = null, CancellationToken ct = default)
        {
            await Task.Run(() =>
            {
                var list = RunVBoxManage("list vms");
                if (!list.Contains(_settings.WhonixGatewayVmName) && File.Exists(_settings.WhonixGatewayOvaPath))
                {
                    progress?.Report(new AnonymityProgress(2, 4, "Importing Whonix-Gateway OVA into VirtualBox..."));
                    RunVBoxManage($"import \"{_settings.WhonixGatewayOvaPath}\"");
                }
                if (!list.Contains(_settings.WhonixWorkstationVmName) && File.Exists(_settings.WhonixWorkstationOvaPath))
                {
                    progress?.Report(new AnonymityProgress(2, 4, "Importing Whonix-Workstation OVA into VirtualBox..."));
                    RunVBoxManage($"import \"{_settings.WhonixWorkstationOvaPath}\"");
                }
            }, ct);
        }

        public async Task ConfigureNetworksAsync()
        {
            await Task.Run(() =>
            {
                // Set Gateway: nic1=NAT, nic2=intnet
                RunVBoxManage($"modifyvm \"{_settings.WhonixGatewayVmName}\" --nic1 nat --nic2 intnet --intnet2 \"{_settings.WhonixInternalNetName}\"");

                // Set Workstation: nic1=intnet, nic2=none
                RunVBoxManage($"modifyvm \"{_settings.WhonixWorkstationVmName}\" --nic1 intnet --intnet1 \"{_settings.WhonixInternalNetName}\" --nic2 none");

                // Add SOCKS5 Port Forwarding rule on Gateway
                try
                {
                    RunVBoxManage($"modifyvm \"{_settings.WhonixGatewayVmName}\" --natpf1 delete \"tor-socks\"");
                }
                catch { }
                RunVBoxManage($"modifyvm \"{_settings.WhonixGatewayVmName}\" --natpf1 \"tor-socks,tcp,127.0.0.1,{_settings.TorGatewayHostPort},,9050\"");

                // Verify topology
                var gwInfo = RunVBoxManage($"showvminfo \"{_settings.WhonixGatewayVmName}\"");
                var wsInfo = RunVBoxManage($"showvminfo \"{_settings.WhonixWorkstationVmName}\"");
                VmTopologyChecker.VerifyTopology(gwInfo, wsInfo);
            });
        }

        public async Task StartGatewayAsync()
        {
            await Task.Run(async () =>
            {
                RunVBoxManage($"startvm \"{_settings.WhonixGatewayVmName}\" --type headless");

                // Poll 127.0.0.1:9050 up to 180s
                int timeout = _settings.GatewayBootstrapTimeoutSeconds;
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalSeconds < timeout)
                {
                    if (await CheckPortAsync("127.0.0.1", _settings.TorGatewayHostPort))
                    {
                        return;
                    }
                    await Task.Delay(2000);
                }

                throw new TimeoutException($"ERR_TOR_TIMEOUT: Whonix-Gateway Tor bootstrap failed on port {_settings.TorGatewayHostPort} within {timeout}s.");
            });
        }

        public async Task StartWorkstationAsync()
        {
            await Task.Run(() =>
            {
                RunVBoxManage($"startvm \"{_settings.WhonixWorkstationVmName}\" --type gui");
            });
        }

        public async Task StopAllAsync()
        {
            await Task.Run(() =>
            {
                TryPowerOff(_settings.WhonixWorkstationVmName);
                TryPowerOff(_settings.WhonixGatewayVmName);

                try
                {
                    RunVBoxManage($"modifyvm \"{_settings.WhonixGatewayVmName}\" --natpf1 delete \"tor-socks\"");
                }
                catch { }
            });
        }

        private void TryPowerOff(string vmName)
        {
            try
            {
                RunVBoxManage($"controlvm \"{vmName}\" acpipowerbutton");
                Thread.Sleep(3000);
            }
            catch { }

            try
            {
                RunVBoxManage($"controlvm \"{vmName}\" poweroff");
            }
            catch { }
        }

        public async Task SnapshotAsync(string name = "clean")
        {
            await Task.Run(() =>
            {
                try { RunVBoxManage($"snapshot \"{_settings.WhonixGatewayVmName}\" take \"{name}\""); } catch { }
                try { RunVBoxManage($"snapshot \"{_settings.WhonixWorkstationVmName}\" take \"{name}\""); } catch { }
            });
        }

        public async Task RestoreSnapshotAsync(string name = "clean")
        {
            await Task.Run(() =>
            {
                try { RunVBoxManage($"snapshot \"{_settings.WhonixGatewayVmName}\" restore \"{name}\""); } catch { }
                try { RunVBoxManage($"snapshot \"{_settings.WhonixWorkstationVmName}\" restore \"{name}\""); } catch { }
            });
        }

        private static string RunVBoxManage(string args)
        {
            var psi = new ProcessStartInfo("vboxmanage", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi) ?? throw new FileNotFoundException("ERR_NO_VBOX: VBoxManage not found.");
            var outText = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(10000);
            return outText;
        }

        private static async Task<bool> CheckPortAsync(string host, int port)
        {
            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(1000)) == connectTask)
                {
                    return tcp.Connected;
                }
            }
            catch { }
            return false;
        }
    }
}
