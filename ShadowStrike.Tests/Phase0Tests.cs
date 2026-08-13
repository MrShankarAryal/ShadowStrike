using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ShadowStrike.Core.Platform.Audit;
using ShadowStrike.Core.Platform.Engagement;
using ShadowStrike.Core.Platform.Reporting;
using ShadowStrike.Core.Platform.Rollback;
using ShadowStrike.Core.Platform.Verification;
using Xunit;

namespace ShadowStrike.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // ScopeValidator tests
    // ─────────────────────────────────────────────────────────────────────────
    public class Phase0ScopeValidatorTests
    {
        private static string BuildPolicyJson(
            string host             = "app.acme.com",
            string mode             = "sqli",
            bool   destructiveTarget = false,
            bool   destructiveGlobal = false,
            DateTime? validFrom     = null,
            DateTime? validUntil    = null)
        {
            var policy = new
            {
                engagement       = "TEST-001",
                client           = "ACME Corp",
                @operator        = "tester@example.com",
                validFrom        = (validFrom  ?? DateTime.UtcNow.AddHours(-1)).ToString("o"),
                validUntil       = (validUntil ?? DateTime.UtcNow.AddHours(+1)).ToString("o"),
                targets          = new[] { new { host, protocols = new[] { "https" }, maxRps = 50, destructive = destructiveTarget } },
                allowedModes     = new[] { mode },
                destructiveTests = destructiveGlobal,
                signature        = ""   // zero-key development mode — skips Ed25519 check
            };
            return JsonSerializer.Serialize(policy);
        }

        private static string WriteTempPolicy(string json)
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, json);
            return path;
        }

        /// <summary>
        /// Reset the ScopeValidator singleton so each test gets a clean instance.
        /// Uses reflection — acceptable in test code.
        /// </summary>
        private static ScopeValidator FreshValidator(string? policyJson = null)
        {
            var field = typeof(ScopeValidator)
                .GetField("_instance",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            field.SetValue(null, null);

            var sv = ScopeValidator.Instance;
            if (policyJson is not null)
            {
                string path = WriteTempPolicy(policyJson);
                try   { sv.LoadPolicy(path); }
                finally { File.Delete(path); }
            }
            return sv;
        }

        [Fact]
        public void IsAllowed_WithNoPolicy_ReturnsFalse()
        {
            ScopeValidator sv = FreshValidator(null);
            Assert.False(sv.IsAllowed("app.acme.com", "sqli", "union"));
        }

        [Fact]
        public void IsAllowed_ValidTargetAndMode_ReturnsTrue()
        {
            ScopeValidator sv = FreshValidator(BuildPolicyJson("app.acme.com", "sqli"));
            Assert.True(sv.IsAllowed("app.acme.com", "sqli", "union"));
        }

        [Fact]
        public void IsAllowed_OutOfScopeTarget_ReturnsFalse()
        {
            ScopeValidator sv = FreshValidator(BuildPolicyJson("app.acme.com", "sqli"));
            Assert.False(sv.IsAllowed("evil.example.com", "sqli", "union"));
        }

        [Fact]
        public void IsAllowed_DisallowedMode_ReturnsFalse()
        {
            ScopeValidator sv = FreshValidator(BuildPolicyJson("app.acme.com", "sqli"));
            Assert.False(sv.IsAllowed("app.acme.com", "xxe", "entity"));
        }

        [Fact]
        public void IsAllowed_ExpiredEngagement_ReturnsFalse()
        {
            string json = BuildPolicyJson(
                validFrom:  DateTime.UtcNow.AddDays(-10),
                validUntil: DateTime.UtcNow.AddDays(-1));
            ScopeValidator sv = FreshValidator(json);
            Assert.False(sv.IsAllowed("app.acme.com", "sqli", "union"));
        }

        [Fact]
        public void IsAllowed_WildcardSubdomain_Matches()
        {
            ScopeValidator sv = FreshValidator(BuildPolicyJson("*.acme.com", "xss"));
            Assert.True(sv.IsAllowed("api.acme.com", "xss", "reflected"));
        }

        [Fact]
        public void IsAllowed_DestructiveTechnique_BlockedWhenNotAllowed()
        {
            string json = BuildPolicyJson("app.acme.com", "sqli",
                destructiveTarget: false, destructiveGlobal: false);
            ScopeValidator sv = FreshValidator(json);
            Assert.False(sv.IsAllowed("app.acme.com", "sqli", "drop"));
        }

        [Fact]
        public void LoadPolicy_MissingEngagementField_Throws()
        {
            string path = WriteTempPolicy("{\"engagement\":\"\",\"client\":\"x\",\"operator\":\"y\"," +
                "\"validFrom\":\"2020-01-01T00:00:00Z\",\"validUntil\":\"2025-01-01T00:00:00Z\"," +
                "\"targets\":[],\"allowedModes\":[],\"destructiveTests\":false,\"signature\":\"\"}");

            var field = typeof(ScopeValidator)
                .GetField("_instance",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            field.SetValue(null, null);

            try
            {
                Assert.Throws<InvalidOperationException>(
                    () => ScopeValidator.Instance.LoadPolicy(path));
            }
            finally { File.Delete(path); }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VerificationEngine tests
    // ─────────────────────────────────────────────────────────────────────────
    public class Phase0VerificationEngineTests
    {
        [Fact]
        public void WelchTest_SignificantDifference_Detected()
        {
            var rng      = new Random(42);
            var baseline = GenerateSamples(rng, mean: 100.0, stdDev: 10.0, n: 20);
            var injected = GenerateSamples(rng, mean: 600.0, stdDev: 15.0, n: 20);

            VerificationResult result = VerificationEngine.Instance
                .RunWelchTest("blind-sleep-sqli", baseline, injected, attachProof: true);

            Assert.True(result.IsSignificant,  $"Expected significant. p={result.PValue:F6}");
            Assert.True(result.PValue < 0.05,  $"p should be < 0.05, got {result.PValue:F6}");
            Assert.NotNull(result.Proof);
            Assert.Equal(20, result.Proof!.SamplesA.Length);
            Assert.Equal(20, result.Proof!.SamplesB.Length);
        }

        [Fact]
        public void WelchTest_NoDifference_NotSignificant()
        {
            var rng      = new Random(99);
            var baseline = GenerateSamples(rng, mean: 100.0, stdDev: 10.0, n: 30);
            var injected = GenerateSamples(rng, mean: 101.0, stdDev: 10.0, n: 30);

            VerificationResult result = VerificationEngine.Instance
                .RunWelchTest("no-injection", baseline, injected, attachProof: false);

            Assert.False(result.IsSignificant, $"Should not be significant. p={result.PValue:F6}");
        }

        [Fact]
        public void WelchTest_TooFewSamples_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => VerificationEngine.Instance
                    .RunWelchTest("short", new double[] { 1.0 }, new double[] { 2.0 }));
        }

        private static double[] GenerateSamples(Random rng, double mean, double stdDev, int n)
        {
            var result = new double[n];
            for (int i = 0; i < n; i++)
            {
                double u1 = 1.0 - rng.NextDouble();
                double u2 = 1.0 - rng.NextDouble();
                double z  = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                result[i] = mean + stdDev * z;
            }
            return result;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RollbackManager tests
    // ─────────────────────────────────────────────────────────────────────────
    public class Phase0RollbackManagerTests
    {
        [Fact]
        public void EnsurePlanExists_WithNoPlan_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => RollbackManager.Instance.EnsurePlanExists("no-such-mode-" + Guid.NewGuid()));
        }

        [Fact]
        public async Task ExecutePlan_CustomAction_Invoked()
        {
            bool executed = false;
            var plan = new RollbackPlan
            {
                ModeId       = "test-mode-" + Guid.NewGuid(),
                Description  = "unit test custom plan",
                Strategy     = RollbackStrategy.Custom,
                CustomAction = () => { executed = true; return Task.CompletedTask; }
            };

            string planId = RollbackManager.Instance.RegisterPlan(plan);
            RollbackManager.Instance.EnsurePlanExists(plan.ModeId);   // should not throw
            await RollbackManager.Instance.ExecutePlanAsync(planId);

            Assert.True(executed, "CustomAction should have been invoked.");
            Assert.True(plan.Executed);
        }

        [Fact]
        public void RegisterPlan_CustomWithNoAction_Throws()
        {
            var plan = new RollbackPlan
            {
                ModeId   = "bad-mode",
                Strategy = RollbackStrategy.Custom
                // CustomAction intentionally null
            };
            Assert.Throws<ArgumentException>(
                () => RollbackManager.Instance.RegisterPlan(plan));
        }

        [Fact]
        public void DeployFileCanary_ExistingFile_RecordsHash()
        {
            string tmp = Path.GetTempFileName();
            File.WriteAllText(tmp, "canary content");
            try
            {
                string canaryId = RollbackManager.Instance.DeployFileCanary(tmp);
                Assert.False(string.IsNullOrEmpty(canaryId));
            }
            finally { File.Delete(tmp); }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AuditLogger tests
    // ─────────────────────────────────────────────────────────────────────────
    public class Phase0AuditLoggerTests
    {
        [Fact]
        public void Log_WritesSingleEvent_BufferContainsIt()
        {
            string marker = "unit-test-marker-" + Guid.NewGuid().ToString("N")[..8];
            AuditLogger.Instance.Log(AuditEventType.SystemInfo, marker);

            bool found = false;
            foreach (var e in AuditLogger.Instance.GetBufferedEvents())
                if (e.Message.Contains(marker)) { found = true; break; }

            Assert.True(found, "Event should appear in the buffer.");
        }

        [Fact]
        public void Log_SequenceNumbers_AreMonotonicallyIncreasing()
        {
            AuditLogger.Instance.Log(AuditEventType.SystemInfo, "seq-test-A");
            AuditLogger.Instance.Log(AuditEventType.SystemInfo, "seq-test-B");

            long prev = -1;
            foreach (var e in AuditLogger.Instance.GetBufferedEvents())
            {
                Assert.True(e.Seq > prev, $"Seq {e.Seq} not > {prev}");
                prev = e.Seq;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ReportGenerator tests
    // ─────────────────────────────────────────────────────────────────────────
    public class Phase0ReportGeneratorTests
    {
        [Fact]
        public void GenerateReport_ProducesJsonAndMarkdown()
        {
            ReportGenerator.Instance.AddFinding(new Finding
            {
                Title       = "Test Finding",
                Target      = "app.test.com",
                Mode        = "sqli",
                Technique   = "union",
                Severity    = FindingSeverity.High,
                Description = "Test description.",
                Evidence    = "HTTP/1.1 200 OK ...",
                Remediation = "Use parameterised queries."
            });

            string outDir = Path.Combine(
                Path.GetTempPath(),
                "ss_report_test_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(outDir);

            try
            {
                (string jsonPath, string mdPath) = ReportGenerator.Instance.GenerateReport(outDir);

                Assert.True(File.Exists(jsonPath),  "JSON report should exist.");
                Assert.True(File.Exists(mdPath),    "Markdown report should exist.");
                Assert.True(new FileInfo(jsonPath).Length > 0);
                Assert.True(new FileInfo(mdPath).Length   > 0);

                string md = File.ReadAllText(mdPath);
                Assert.Contains("ShadowStrike", md);
            }
            finally
            {
                Directory.Delete(outDir, recursive: true);
            }
        }
    }
}
