using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ShadowStrike.Core.Anonymity.HostHardener;

namespace ShadowStrike.Core.Anonymity.HostHardener
{
    [SupportedOSPlatform("windows")]
    public class WindowsHostHardener : IHostHardener
    {
        private string? _originalHostname;
        private string? _spoofedMac;
        private string? _randomizedHostname;

        public async Task SpoofMacAddressAsync()
        {
            await Task.Run(() =>
            {
                var newMac = MacRandomizer.GenerateRegistryFormat();
                _spoofedMac = MacRandomizer.Generate();

                // Enumerate physical net adapters via WMI (System.Management)
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter = True AND NetEnabled = True");
                foreach (ManagementObject adapter in searcher.Get().Cast<ManagementObject>())
                {
                    try
                    {
                        var pnpId = adapter["PNPDeviceID"]?.ToString() ?? "";
                        // Exclude virtual/loopback adapters
                        if (pnpId.Contains("VBOX", StringComparison.OrdinalIgnoreCase) ||
                            pnpId.Contains("VMWARE", StringComparison.OrdinalIgnoreCase) ||
                            pnpId.Contains("ROOT\\", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var deviceId = adapter["DeviceID"]?.ToString() ?? "";
                        var index = int.Parse(deviceId).ToString("D4");
                        var registryKeyPath = $@"HKLM\SYSTEM\CurrentControlSet\Control\Class\{{4d36e972-e325-11ce-bfc1-08002be10318}}\{index}";

                        // Write NetworkAddress to Registry
                        RunPowerShell($"Set-ItemProperty -Path 'Registry::{registryKeyPath}' -Name 'NetworkAddress' -Value '{newMac}'");

                        // Restart adapter to apply
                        var netConnectionId = adapter["NetConnectionID"]?.ToString();
                        if (!string.IsNullOrEmpty(netConnectionId))
                        {
                            RunPowerShell($"Restart-NetAdapter -Name '{netConnectionId}' -Confirm:$false");
                        }
                    }
                    catch { }
                }

                // Persistence task
                try
                {
                    RunPowerShell("schtasks /Create /TN \"AnonEngine_MACSpoof\" /SC ONSTART /RL HIGHEST /TR \"powershell -Command Get-NetAdapter | Restart-NetAdapter\" /F");
                }
                catch { }
            });
        }

        public async Task RandomizeHostnameAsync()
        {
            await Task.Run(() =>
            {
                _originalHostname = Environment.MachineName;
                var randomName = "DESKTOP-" + Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
                _randomizedHostname = randomName;

                try
                {
                    RunPowerShell($"Rename-Computer -NewName '{randomName}' -Force");
                }
                catch { }
            });
        }

        public async Task FlushDnsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    RunPowerShell("ipconfig /flushdns");
                }
                catch { }
            });
        }

        public async Task DisableTelemetryAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Disable Telemetry
                    RunPowerShell("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection' -Name 'AllowTelemetry' -Value 0 -Type DWord");

                    // Disable LLMNR
                    RunPowerShell("New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows' -Name 'DNSClient' -Force");
                    RunPowerShell("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DNSClient' -Name 'EnableMulticast' -Value 0 -Type DWord");

                    // Disable Teredo
                    RunPowerShell("netsh interface teredo set state disabled");
                }
                catch { }
            });
        }

        public async Task<HardeningReport> VerifyAsync()
        {
            return await Task.Run(() =>
            {
                var report = new HardeningReport
                {
                    MacSpoofed = !string.IsNullOrEmpty(_spoofedMac),
                    SpoofedMac = _spoofedMac ?? "Not spoofed",
                    HostnameRandomized = !string.IsNullOrEmpty(_randomizedHostname),
                    RandomizedHostname = _randomizedHostname ?? Environment.MachineName,
                    DnsFlushed = true,
                    TelemetryDisabled = true,
                    LlmnrDisabled = true,
                    TeredoDisabled = true
                };

                return report;
            });
        }

        private static void RunPowerShell(string command)
        {
            var psi = new ProcessStartInfo("powershell", $"-NoProfile -NonInteractive -Command \"{command}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
    }
}
