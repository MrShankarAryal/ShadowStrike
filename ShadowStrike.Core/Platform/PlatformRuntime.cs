using System;
using System.IO;
using ShadowStrike.Core.Platform.Audit;
using ShadowStrike.Core.Platform.Engagement;
using ShadowStrike.Core.Platform.Reporting;
using ShadowStrike.Core.Platform.Rollback;
using ShadowStrike.Core.Platform.Verification;

namespace ShadowStrike.Core.Platform
{
    /// <summary>
    /// Phase 0 platform bootstrap.
    /// Call <see cref="Initialize"/> once at application startup before any mode runs.
    /// After initialization:
    ///   - ScopeValidator.Instance  — the choke-point for all attack traffic
    ///   - AuditLogger.Instance     — tamper-evident log chain
    ///   - VerificationEngine.Instance — statistical t-test engine
    ///   - RollbackManager.Instance — canary monitor + rollback execution
    ///   - ReportGenerator.Instance — report builder
    /// </summary>
    public static class PlatformRuntime
    {
        private static bool _initialized = false;
        private static readonly object _lock = new();

        /// <summary>
        /// Bootstrap all Phase 0 subsystems.
        /// </summary>
        /// <param name="engagementJsonPath">
        ///   Path to the signed engagement.json file.
        ///   Pass <c>null</c> to run without a policy (all requests will be blocked by ScopeValidator).
        /// </param>
        public static void Initialize(string? engagementJsonPath = null)
        {
            lock (_lock)
            {
                if (_initialized) return;

                // 1. AuditLogger starts itself on first access (logs to %APPDATA%\ShadowStrike\audit\).
                AuditLogger.Instance.Log(AuditEventType.SystemInfo,
                    $"PlatformRuntime initializing. " +
                    $"OS={Environment.OSVersion} CLR={Environment.Version} " +
                    $"Host={Environment.MachineName} User={Environment.UserName}");

                // 2. Load and verify the engagement policy.
                if (!string.IsNullOrWhiteSpace(engagementJsonPath))
                {
                    ScopeValidator.Instance.LoadPolicy(engagementJsonPath);
                }
                else
                {
                    AuditLogger.Instance.Log(AuditEventType.Warning,
                        "No engagement JSON supplied — ScopeValidator will block all requests (fail-closed).");
                }

                // 3. Touch VerificationEngine so it is warmed up.
                _ = VerificationEngine.Instance;

                // 4. Start RollbackManager canary monitor.
                _ = RollbackManager.Instance;

                // 5. Touch ReportGenerator.
                _ = ReportGenerator.Instance;

                _initialized = true;

                AuditLogger.Instance.Log(AuditEventType.SystemInfo,
                    "PlatformRuntime initialized successfully. All Phase-0 subsystems online.");
            }
        }

        /// <summary>
        /// Guard method — call at the top of every mode's Execute() method.
        /// Verifies scope, mode, and technique are permitted, then checks a rollback plan exists.
        /// Throws <see cref="InvalidOperationException"/> on any violation.
        /// </summary>
        public static void GuardExecution(string target, string mode, string technique, bool isMutation = false)
        {
            if (!_initialized)
                throw new InvalidOperationException(
                    "PlatformRuntime.Initialize() must be called before any mode executes.");

            // Scope check — hard block.
            if (!ScopeValidator.Instance.IsAllowed(target, mode, technique))
                throw new InvalidOperationException(
                    $"Scope violation: target='{target}' mode='{mode}' technique='{technique}' " +
                    "is blocked by the engagement policy. See audit log for details.");

            // Rollback plan check for mutation modes.
            if (isMutation)
                RollbackManager.Instance.EnsurePlanExists(mode);
        }

        /// <summary>Returns true when the platform is initialized with a valid engagement policy.</summary>
        public static bool IsOperational =>
            _initialized && ScopeValidator.Instance.IsLoaded;

        /// <summary>Flush the engagement report at session end.</summary>
        public static (string JsonPath, string MarkdownPath) FinalizeReport(string? outputDir = null)
        {
            var paths = ReportGenerator.Instance.GenerateReport(outputDir);
            AuditLogger.Instance.Log(AuditEventType.SystemInfo, "PlatformRuntime session finalized.");
            return paths;
        }
    }
}
