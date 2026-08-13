using System;
using System.Threading;
using System.Threading.Tasks;
using ShadowStrike.Core.Anonymity.HostHardener;
using ShadowStrike.Core.Anonymity.LeakGuardSystem;
using ShadowStrike.Core.Anonymity.Network;
using ShadowStrike.Core.Anonymity.VmOrchestrator;

namespace ShadowStrike.Core.Anonymity
{
    public class AnonymityEngine
    {
        private static readonly Lazy<AnonymityEngine> _instance = new(() => new AnonymityEngine());
        public static AnonymityEngine Instance => _instance.Value;

        private readonly AnonymitySettings _settings;
        private readonly IHostHardener _hardener;
        private readonly IVmOrchestrator _orchestrator;
        private readonly ILeakGuard _leakGuard;
        private PeriodicTimer? _leakTimer;
        private CancellationTokenSource? _leakCts;
        private Thread? _watchdogThread;

        public AnonymityMode Mode { get; private set; } = AnonymityMode.Off;
        public event EventHandler<LeakReport>? LeakDetected;

        private AnonymityEngine()
        {
            _settings = AnonymitySettings.Load();
            _hardener = HostHardenerFactory.Create();
            _orchestrator = new WhonixOrchestrator(_settings);
            _leakGuard = new LeakGuard(_orchestrator);

            // Setup uncatchable watchdog thread (non-background, dedicated)
            SetupWatchdog();
        }

        private void SetupWatchdog()
        {
            _watchdogThread = new Thread(() =>
            {
                AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                {
                    DisableModeAsync().GetAwaiter().GetResult();
                };
            })
            {
                IsBackground = false, // Never GC'd, uncatchable watchdog
                Name = "ShadowStrike_AnonymityWatchdog"
            };
        }

        public async Task EnableLightweightModeAsync(IProgress<AnonymityProgress>? progress = null, CancellationToken ct = default)
        {
            progress?.Report(new AnonymityProgress(1, 3, "Starting lightweight Tor process..."));
            bool started = await TorManager.StartTorAsync(_settings);
            if (!started)
            {
                throw new InvalidOperationException("ERR_TOR_TIMEOUT: Failed to start tor.exe lightweight process.");
            }

            ProxyManager.Instance.Activate("127.0.0.1", TorManager.TorPort);
            TorManager.StartRotationService(7);
            Mode = AnonymityMode.LightweightTor;

            progress?.Report(new AnonymityProgress(2, 3, "Verifying Tor connection..."));
            var report = await _leakGuard.PreFlightAsync();
            if (report.Verdict == "FAIL")
            {
                await DisableModeAsync();
                throw new InvalidOperationException($"ERR_LEAK: Pre-flight check failed — {report.FailureDetails}");
            }

            StartLeakMonitoring();
            progress?.Report(new AnonymityProgress(3, 3, "Lightweight Anonymous Mode Active ✓"));
        }

        public async Task EnableHardenedModeAsync(IProgress<AnonymityProgress>? progress = null, CancellationToken ct = default)
        {
            try
            {
                progress?.Report(new AnonymityProgress(1, 6, "Hardening host (MAC, Hostname, DNS, Telemetry)..."));
                await _hardener.SpoofMacAddressAsync();
                await _hardener.RandomizeHostnameAsync();
                await _hardener.FlushDnsAsync();
                await _hardener.DisableTelemetryAsync();

                progress?.Report(new AnonymityProgress(2, 6, "Checking VirtualBox & Whonix images..."));
                bool vbox = await _orchestrator.DetectVirtualBoxAsync();
                if (!vbox) throw new InvalidOperationException("ERR_NO_VBOX: VirtualBox is not installed or VBoxManage is not in PATH.");

                await _orchestrator.EnsureImagesPresentAsync(progress, ct);
                await _orchestrator.ImportImagesAsync(progress, ct);

                progress?.Report(new AnonymityProgress(3, 6, "Configuring network topology..."));
                await _orchestrator.ConfigureNetworksAsync();

                progress?.Report(new AnonymityProgress(4, 6, "Starting Whonix-Gateway VM..."));
                await _orchestrator.StartGatewayAsync();

                progress?.Report(new AnonymityProgress(5, 6, "Activating SOCKS5 proxy tunnel..."));
                ProxyManager.Instance.Activate("127.0.0.1", _settings.TorGatewayHostPort);
                Mode = AnonymityMode.HardenedWhonix;

                progress?.Report(new AnonymityProgress(6, 6, "Running Pre-Flight Leak Guard..."));
                var report = await _leakGuard.PreFlightAsync();
                if (report.Verdict == "FAIL")
                {
                    await DisableModeAsync();
                    throw new InvalidOperationException($"ERR_LEAK: Hardened Pre-Flight failed — {report.FailureDetails}");
                }

                StartLeakMonitoring();
            }
            catch
            {
                await DisableModeAsync();
                throw;
            }
        }

        public async Task DisableModeAsync()
        {
            try
            {
                StopLeakMonitoring();
                ProxyManager.Instance.Deactivate();

                if (Mode == AnonymityMode.LightweightTor)
                {
                    TorManager.StopRotationService();
                    TorManager.StopTor();
                }
                else if (Mode == AnonymityMode.HardenedWhonix)
                {
                    await _orchestrator.StopAllAsync();
                    await _orchestrator.RestoreSnapshotAsync("clean");
                }
            }
            finally
            {
                Mode = AnonymityMode.Off;
            }
        }

        private void StartLeakMonitoring()
        {
            StopLeakMonitoring();
            _leakCts = new CancellationTokenSource();
            _leakTimer = new PeriodicTimer(TimeSpan.FromSeconds(60));

            Task.Run(async () =>
            {
                while (_leakTimer != null && await _leakTimer.WaitForNextTickAsync(_leakCts.Token))
                {
                    var report = await _leakGuard.RuntimeCheckAsync();
                    if (report.Verdict == "FAIL")
                    {
                        LeakDetected?.Invoke(this, report);
                        await DisableModeAsync();
                        break;
                    }
                }
            }, _leakCts.Token);
        }

        private void StopLeakMonitoring()
        {
            _leakCts?.Cancel();
            _leakTimer?.Dispose();
            _leakTimer = null;
            _leakCts = null;
        }
    }
}
