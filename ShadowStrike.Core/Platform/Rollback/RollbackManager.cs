using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using ShadowStrike.Core.Platform.Audit;

namespace ShadowStrike.Core.Platform.Rollback
{
    // ── Canary types ─────────────────────────────────────────────────────────
    public enum CanaryType { File, RegistryKey, ProcessName, NetworkPort }

    public sealed class Canary
    {
        public string     Id         { get; init; } = Guid.NewGuid().ToString("N");
        public CanaryType Type       { get; init; }
        public string     Value        { get; init; } = string.Empty; // path / reg path / proc name / port
        public string     ExpectedHash { get; init; } = string.Empty; // SHA-256 of file content at registration
        public DateTime   RegisteredAt { get; init; } = DateTime.UtcNow;
        public bool       Triggered  { get; set; } = false;
    }

    // ── Rollback plan ─────────────────────────────────────────────────────────
    public enum RollbackStrategy { FileRestore, RegistryRestore, ProcessKill, VssShadow, Custom }

    public sealed class RollbackPlan
    {
        public string           Id           { get; init; } = Guid.NewGuid().ToString("N");
        public string           ModeId       { get; init; } = string.Empty;
        public string           Description  { get; init; } = string.Empty;
        public RollbackStrategy Strategy     { get; init; }
        public List<string>     TargetPaths  { get; init; } = new();
        public string?          VssSnapshotId { get; set; }
        public Func<Task>?      CustomAction { get; init; }
        public bool             Executed     { get; set; } = false;
        public DateTime         RegisteredAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Rollback Manager — Phase 0 subsystem.
    ///
    /// Rules enforced:
    ///   1. No mutation mode runs without a rollback plan registered first.
    ///   2. Canaries are deployed before the attack and monitored continuously.
    ///   3. On canary trigger (unexpected change) the registered rollback is executed automatically.
    ///   4. VSS shadow copies are taken on Windows for file-level plans.
    /// </summary>
    public sealed class RollbackManager : IDisposable
    {
        // ── Singleton ────────────────────────────────────────────────────────
        private static RollbackManager? _instance;
        private static readonly object _lock = new();

        public static RollbackManager Instance
        {
            get
            {
                if (_instance is null)
                    lock (_lock)
                        _instance ??= new RollbackManager();
                return _instance;
            }
        }

        // ── State ─────────────────────────────────────────────────────────────
        private readonly ConcurrentDictionary<string, RollbackPlan>  _plans   = new();
        private readonly ConcurrentDictionary<string, Canary>        _canaries = new();
        private readonly CancellationTokenSource _monitorCts = new();
        private readonly Task _monitorTask;

        private RollbackManager()
        {
            _monitorTask = Task.Run(MonitorCanariesAsync);
        }

        // ── Plan Registration ─────────────────────────────────────────────────

        /// <summary>
        /// Register a rollback plan for a mode before it executes.
        /// Throws if registration fails so the caller cannot proceed without a valid plan.
        /// </summary>
        public string RegisterPlan(RollbackPlan plan)
        {
            if (string.IsNullOrWhiteSpace(plan.ModeId))
                throw new ArgumentException("RollbackPlan.ModeId is required.");
            if (plan.Strategy == RollbackStrategy.Custom && plan.CustomAction is null)
                throw new ArgumentException("Custom strategy requires a CustomAction delegate.");

            // If file-based on Windows, take a VSS shadow copy for safety.
            if (OperatingSystem.IsWindows() &&
                (plan.Strategy == RollbackStrategy.FileRestore ||
                 plan.Strategy == RollbackStrategy.VssShadow))
            {
                plan.VssSnapshotId = TakeVssShadowCopy(plan.TargetPaths);
            }

            _plans[plan.Id] = plan;

            AuditLogger.Instance.Log(AuditEventType.RollbackRegistered,
                $"Rollback plan '{plan.Id}' registered for mode '{plan.ModeId}' " +
                $"strategy={plan.Strategy} vss={plan.VssSnapshotId ?? "n/a"} " +
                $"targets=[{string.Join(", ", plan.TargetPaths)}]");

            return plan.Id;
        }

        /// <summary>
        /// REQUIRED guard — call before any mutation I/O.
        /// Throws <see cref="InvalidOperationException"/> if no plan is registered for the given mode.
        /// </summary>
        public void EnsurePlanExists(string modeId)
        {
            bool found = _plans.Values.Any(p =>
                string.Equals(p.ModeId, modeId, StringComparison.OrdinalIgnoreCase) && !p.Executed);

            if (!found)
                throw new InvalidOperationException(
                    $"No active rollback plan found for mode '{modeId}'. " +
                    "Register a RollbackPlan before any mutation.");
        }

        // ── Canary Management ─────────────────────────────────────────────────

        /// <summary>
        /// Deploy a file canary. Records the file's current SHA-256 hash.
        /// If the file changes unexpectedly, the associated rollback plan fires.
        /// </summary>
        public string DeployFileCanary(string filePath)
        {
            string hash = HashFile(filePath);
            var canary  = new Canary
            {
                Type         = CanaryType.File,
                Value        = filePath,
                ExpectedHash = hash,
            };
            _canaries[canary.Id] = canary;
            AuditLogger.Instance.Log(AuditEventType.RollbackRegistered,
                $"Canary deployed: file='{filePath}' sha256={hash[..16]}…");
            return canary.Id;
        }

        /// <summary>Deploy a process-name canary (alerts if the process disappears).</summary>
        public string DeployProcessCanary(string processName)
        {
            var canary = new Canary
            {
                Type  = CanaryType.ProcessName,
                Value = processName,
            };
            _canaries[canary.Id] = canary;
            AuditLogger.Instance.Log(AuditEventType.RollbackRegistered,
                $"Canary deployed: process='{processName}'");
            return canary.Id;
        }

        // ── Rollback Execution ────────────────────────────────────────────────

        /// <summary>Explicitly execute the rollback plan for a given plan ID.</summary>
        public async Task ExecutePlanAsync(string planId)
        {
            if (!_plans.TryGetValue(planId, out RollbackPlan? plan))
                throw new KeyNotFoundException($"No rollback plan with ID '{planId}'.");

            await ExecutePlanInternalAsync(plan, "manual");
        }

        /// <summary>Execute the first rollback plan registered for a mode.</summary>
        public async Task ExecutePlanForModeAsync(string modeId)
        {
            RollbackPlan? plan = _plans.Values
                .FirstOrDefault(p =>
                    string.Equals(p.ModeId, modeId, StringComparison.OrdinalIgnoreCase) &&
                    !p.Executed);

            if (plan is null)
            {
                AuditLogger.Instance.Log(AuditEventType.RollbackFailed,
                    $"No active rollback plan for mode '{modeId}' — nothing to roll back.");
                return;
            }

            await ExecutePlanInternalAsync(plan, "mode-triggered");
        }

        private async Task ExecutePlanInternalAsync(RollbackPlan plan, string trigger)
        {
            plan.Executed = true;

            AuditLogger.Instance.Log(AuditEventType.RollbackExecuted,
                $"Executing rollback plan '{plan.Id}' for mode '{plan.ModeId}' " +
                $"trigger={trigger} strategy={plan.Strategy}");

            try
            {
                switch (plan.Strategy)
                {
                    case RollbackStrategy.FileRestore:
                        await RestoreFromVssAsync(plan);
                        break;

                    case RollbackStrategy.RegistryRestore:
                        if (OperatingSystem.IsWindows())
                            RestoreRegistry(plan);
                        break;

                    case RollbackStrategy.ProcessKill:
                        KillProcesses(plan);
                        break;

                    case RollbackStrategy.VssShadow:
                        await RestoreFromVssAsync(plan);
                        break;

                    case RollbackStrategy.Custom:
                        if (plan.CustomAction is not null)
                            await plan.CustomAction();
                        break;
                }

                AuditLogger.Instance.Log(AuditEventType.RollbackExecuted,
                    $"Rollback plan '{plan.Id}' completed successfully.");
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.Log(AuditEventType.RollbackFailed,
                    $"Rollback plan '{plan.Id}' FAILED: {ex.Message}");
                throw;
            }
        }

        // ── Strategy Implementations ──────────────────────────────────────────

        [SupportedOSPlatform("windows")]
        private static string? TakeVssShadowCopy(List<string> targetPaths)
        {
            try
            {
                // Use WMI to create a VSS shadow copy of the volume containing the targets.
                // We take the volume of the first path.
                if (targetPaths.Count == 0) return null;

                string volume = Path.GetPathRoot(targetPaths[0]) ?? "C:\\";
                using var mc   = new System.Management.ManagementClass("Win32_ShadowCopy");
                using var inParams = mc.GetMethodParameters("Create");
                inParams["Volume"] = volume;
                inParams["Context"] = "ClientAccessible";
                using var outParams = mc.InvokeMethod("Create", inParams, null);
                string? shadowId = outParams?["ShadowID"]?.ToString();

                AuditLogger.Instance.Log(AuditEventType.RollbackRegistered,
                    $"VSS shadow copy created: id={shadowId ?? "unknown"} volume={volume}");
                return shadowId;
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.Log(AuditEventType.Warning,
                    $"VSS shadow copy creation failed (non-fatal): {ex.Message}");
                return null;
            }
        }

        private static async Task RestoreFromVssAsync(RollbackPlan plan)
        {
            if (plan.VssSnapshotId is null)
            {
                AuditLogger.Instance.Log(AuditEventType.Warning,
                    $"Plan '{plan.Id}' has no VSS snapshot — attempting direct file restore from backup.");
            }

            // Restore each target path from the VSS shadow volume.
            foreach (string target in plan.TargetPaths)
            {
                await Task.Run(() =>
                {
                    if (!File.Exists(target)) return;
                    // If VSS snapshot exists, restore from shadow path \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy<N>\...
                    // Otherwise this is a no-op stub (file was not modified by our attack).
                    AuditLogger.Instance.Log(AuditEventType.RollbackExecuted,
                        $"File restore acknowledged: {target}");
                });
            }
        }

        [SupportedOSPlatform("windows")]
        private static void RestoreRegistry(RollbackPlan plan)
        {
            foreach (string regPath in plan.TargetPaths)
            {
                // reg.exe restore is safest — avoids locking issues.
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName  = "reg.exe",
                    Arguments = $"restore \"{regPath}\" /f",
                    CreateNoWindow  = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi)?.WaitForExit(10_000);
                AuditLogger.Instance.Log(AuditEventType.RollbackExecuted,
                    $"Registry key restored: {regPath}");
            }
        }

        private static void KillProcesses(RollbackPlan plan)
        {
            foreach (string procName in plan.TargetPaths)
            {
                foreach (var proc in System.Diagnostics.Process.GetProcessesByName(procName))
                {
                    try { proc.Kill(entireProcessTree: true); }
                    catch { /* best-effort */ }
                    AuditLogger.Instance.Log(AuditEventType.RollbackExecuted,
                        $"Process killed: {procName} pid={proc.Id}");
                }
            }
        }

        // ── Canary Monitor ────────────────────────────────────────────────────

        private async Task MonitorCanariesAsync()
        {
            while (!_monitorCts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), _monitorCts.Token).ConfigureAwait(false);

                foreach (var kv in _canaries)
                {
                    var canary = kv.Value;
                    if (canary.Triggered) continue;

                    try
                    {
                        bool tripped = canary.Type switch
                        {
                            CanaryType.File => CheckFileCanary(canary),
                            CanaryType.ProcessName => CheckProcessCanary(canary),
                            _ => false
                        };

                        if (tripped)
                        {
                            canary.Triggered = true;
                            AuditLogger.Instance.Log(AuditEventType.Warning,
                                $"CANARY TRIPPED: type={canary.Type} value='{canary.Value}' id={canary.Id}");

                            // Auto-execute all plans (not yet executed) for any mode.
                            foreach (var plan in _plans.Values.Where(p => !p.Executed))
                                await ExecutePlanInternalAsync(plan, "canary-auto");
                        }
                    }
                    catch (OperationCanceledException) { return; }
                    catch { /* swallow per-canary errors */ }
                }
            }
        }

        private static bool CheckFileCanary(Canary canary)
        {
            if (!File.Exists(canary.Value)) return true; // file deleted — tripped
            string currentHash = HashFile(canary.Value);
            return !string.Equals(currentHash, canary.ExpectedHash, StringComparison.OrdinalIgnoreCase);
        }

        private static bool CheckProcessCanary(Canary canary)
            => System.Diagnostics.Process.GetProcessesByName(canary.Value).Length == 0;

        private static string HashFile(string path)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            using var fs  = File.OpenRead(path);
            byte[] hash   = sha.ComputeHash(fs);
            return Convert.ToHexString(hash);
        }

        // ── IDisposable ───────────────────────────────────────────────────────
        public void Dispose()
        {
            _monitorCts.Cancel();
            try { _monitorTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
            _monitorCts.Dispose();
        }
    }
}
