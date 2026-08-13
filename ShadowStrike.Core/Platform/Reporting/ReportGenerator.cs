using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ShadowStrike.Core.Platform.Audit;
using ShadowStrike.Core.Platform.Engagement;
using ShadowStrike.Core.Platform.Verification;

namespace ShadowStrike.Core.Platform.Reporting
{
    /// <summary>Severity classification for a finding.</summary>
    public enum FindingSeverity { Info, Low, Medium, High, Critical }

    /// <summary>A single evidence-backed finding in the engagement report.</summary>
    public sealed class Finding
    {
        public string           Id                { get; init; } = Guid.NewGuid().ToString("N")[..8];
        public string           Title             { get; init; } = string.Empty;
        public string           Target            { get; init; } = string.Empty;
        public string           Mode              { get; init; } = string.Empty;
        public string           Technique         { get; init; } = string.Empty;
        public FindingSeverity  Severity          { get; init; }
        public string           Description       { get; init; } = string.Empty;
        public string           Evidence          { get; init; } = string.Empty;
        public string           Remediation       { get; init; } = string.Empty;
        public VerificationResult? VerificationResult { get; init; }
        public DateTime         DiscoveredAt      { get; init; } = DateTime.UtcNow;
        public string           CvssVector        { get; init; } = string.Empty;
    }

    /// <summary>The full engagement report produced at the end of a session.</summary>
    public sealed class EngagementReport
    {
        public string              ReportId      { get; init; } = Guid.NewGuid().ToString("N")[..12];
        public DateTime            GeneratedAt   { get; init; } = DateTime.UtcNow;
        public EngagementPolicy?   Policy        { get; init; }
        public List<Finding>       Findings      { get; init; } = new();
        public List<AuditEvent>    AuditTrail    { get; init; } = new();
        public Dictionary<string, int> Summary   { get; init; } = new();
    }

    /// <summary>
    /// Report generator — Phase 0 subsystem.
    /// Compiles the engagement report from live audit events and registered findings.
    /// Outputs: machine-readable JSON + human-readable Markdown.
    /// </summary>
    public sealed class ReportGenerator
    {
        // ── Singleton ────────────────────────────────────────────────────────
        private static ReportGenerator? _instance;
        private static readonly object _lock = new();

        public static ReportGenerator Instance
        {
            get
            {
                if (_instance is null)
                    lock (_lock)
                        _instance ??= new ReportGenerator();
                return _instance;
            }
        }

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<Finding> _findings = new();
        private readonly object _findingsLock = new();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Register a finding discovered during the engagement.</summary>
        public void AddFinding(Finding finding)
        {
            lock (_findingsLock)
                _findings.Add(finding);
        }

        /// <summary>
        /// Build and write the full engagement report.
        /// </summary>
        /// <param name="outputDir">Directory to write files into.
        ///   Defaults to %APPDATA%\ShadowStrike\reports\</param>
        /// <returns>Paths to the generated JSON and Markdown files.</returns>
        public (string JsonPath, string MarkdownPath) GenerateReport(string? outputDir = null)
        {
            outputDir ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ShadowStrike", "reports");
            Directory.CreateDirectory(outputDir);

            EngagementPolicy? policy = ScopeValidator.Instance.Policy;
            IReadOnlyList<AuditEvent> auditEvents = AuditLogger.Instance.GetBufferedEvents();

            List<Finding> snapshot;
            lock (_findingsLock)
                snapshot = new List<Finding>(_findings);

            var report = new EngagementReport
            {
                Policy     = policy,
                Findings   = snapshot,
                AuditTrail = new List<AuditEvent>(auditEvents),
                Summary    = BuildSummary(snapshot)
            };

            string stamp    = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss");
            string engId    = policy?.Engagement ?? "unknown";
            string baseName = $"report-{engId}-{stamp}";

            string jsonPath = Path.Combine(outputDir, baseName + ".json");
            string mdPath   = Path.Combine(outputDir, baseName + ".md");

            // Write JSON.
            string json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented     = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            File.WriteAllText(jsonPath, json, Encoding.UTF8);

            // Write Markdown.
            string markdown = BuildMarkdown(report);
            File.WriteAllText(mdPath, markdown, Encoding.UTF8);

            AuditLogger.Instance.Log(AuditEventType.ReportGenerated,
                $"Report generated: json={jsonPath} md={mdPath} " +
                $"findings={snapshot.Count} auditEvents={auditEvents.Count}");

            return (jsonPath, mdPath);
        }

        // ── Summary ────────────────────────────────────────────────────────────

        private static Dictionary<string, int> BuildSummary(List<Finding> findings)
        {
            var summary = new Dictionary<string, int>
            {
                { "Total",    findings.Count },
                { "Critical", findings.Count(f => f.Severity == FindingSeverity.Critical) },
                { "High",     findings.Count(f => f.Severity == FindingSeverity.High)     },
                { "Medium",   findings.Count(f => f.Severity == FindingSeverity.Medium)   },
                { "Low",      findings.Count(f => f.Severity == FindingSeverity.Low)      },
                { "Info",     findings.Count(f => f.Severity == FindingSeverity.Info)     },
            };
            return summary;
        }

        // ── Markdown Builder ───────────────────────────────────────────────────

        private static string BuildMarkdown(EngagementReport report)
        {
            var sb = new StringBuilder();
            EngagementPolicy? p = report.Policy;

            // ── Header ────────────────────────────────────────────────────────
            sb.AppendLine("# ShadowStrike Engagement Report");
            sb.AppendLine();
            sb.AppendLine($"| Field | Value |");
            sb.AppendLine($"|-------|-------|");
            sb.AppendLine($"| Report ID | `{report.ReportId}` |");
            sb.AppendLine($"| Generated | {report.GeneratedAt:u} |");
            sb.AppendLine($"| Engagement | {p?.Engagement ?? "—"} |");
            sb.AppendLine($"| Client | {p?.Client ?? "—"} |");
            sb.AppendLine($"| Operator | {p?.Operator ?? "—"} |");
            sb.AppendLine($"| Valid From | {p?.ValidFrom:u} |");
            sb.AppendLine($"| Valid Until | {p?.ValidUntil:u} |");
            sb.AppendLine();

            // ── Executive Summary ─────────────────────────────────────────────
            sb.AppendLine("## Executive Summary");
            sb.AppendLine();
            sb.AppendLine("| Severity | Count |");
            sb.AppendLine("|----------|-------|");
            foreach (var kv in report.Summary)
                sb.AppendLine($"| {kv.Key} | **{kv.Value}** |");
            sb.AppendLine();

            // ── Findings ──────────────────────────────────────────────────────
            sb.AppendLine("## Findings");
            sb.AppendLine();

            if (report.Findings.Count == 0)
            {
                sb.AppendLine("_No findings recorded during this engagement._");
                sb.AppendLine();
            }
            else
            {
                foreach (Finding f in report.Findings.OrderByDescending(x => x.Severity))
                {
                    sb.AppendLine($"### [{f.Id}] {f.Title}");
                    sb.AppendLine();
                    sb.AppendLine($"- **Severity**: {f.Severity}");
                    sb.AppendLine($"- **Target**: `{f.Target}`");
                    sb.AppendLine($"- **Mode**: `{f.Mode}` / **Technique**: `{f.Technique}`");
                    sb.AppendLine($"- **Discovered**: {f.DiscoveredAt:u}");
                    if (!string.IsNullOrWhiteSpace(f.CvssVector))
                        sb.AppendLine($"- **CVSS Vector**: `{f.CvssVector}`");
                    sb.AppendLine();
                    sb.AppendLine($"**Description**");
                    sb.AppendLine();
                    sb.AppendLine(f.Description);
                    sb.AppendLine();
                    sb.AppendLine($"**Evidence**");
                    sb.AppendLine();
                    sb.AppendLine("```");
                    sb.AppendLine(f.Evidence);
                    sb.AppendLine("```");
                    sb.AppendLine();

                    // Attach statistical proof if present.
                    if (f.VerificationResult is VerificationResult vr)
                    {
                        sb.AppendLine("**Statistical Verification (Welch's t-test)**");
                        sb.AppendLine();
                        sb.AppendLine($"| Metric | Value |");
                        sb.AppendLine($"|--------|-------|");
                        sb.AppendLine($"| t-statistic | {vr.TStatistic:F4} |");
                        sb.AppendLine($"| Degrees of freedom | {vr.Df:F2} |");
                        sb.AppendLine($"| p-value | {vr.PValue:F6} |");
                        sb.AppendLine($"| Significant (α=0.05) | {vr.IsSignificant} |");
                        sb.AppendLine($"| Mean baseline (ms) | {vr.MeanA:F2} |");
                        sb.AppendLine($"| Mean injected (ms) | {vr.MeanB:F2} |");
                        sb.AppendLine();
                        sb.AppendLine($"> {vr.Interpretation}");
                        sb.AppendLine();
                    }

                    sb.AppendLine($"**Remediation**");
                    sb.AppendLine();
                    sb.AppendLine(f.Remediation);
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            // ── Scope Violations ──────────────────────────────────────────────
            var violations = report.AuditTrail
                .Where(e => e.Type == AuditEventType.ScopeViolation.ToString())
                .ToList();

            if (violations.Count > 0)
            {
                sb.AppendLine("## Scope Violations (Blocked)");
                sb.AppendLine();
                sb.AppendLine("| # | Timestamp | Message |");
                sb.AppendLine("|---|-----------|---------|");
                for (int i = 0; i < violations.Count; i++)
                    sb.AppendLine($"| {i + 1} | {violations[i].Timestamp} | {violations[i].Message.Replace("|", "\\|")} |");
                sb.AppendLine();
            }

            // ── Audit Trail ───────────────────────────────────────────────────
            sb.AppendLine("## Full Audit Trail");
            sb.AppendLine();
            sb.AppendLine("<details><summary>Click to expand</summary>");
            sb.AppendLine();
            sb.AppendLine("| # | Timestamp | Type | Message |");
            sb.AppendLine("|---|-----------|------|---------|");
            foreach (var evt in report.AuditTrail)
                sb.AppendLine(
                    $"| {evt.Seq} | {evt.Timestamp} | {evt.Type} | {evt.Message.Replace("|", "\\|")} |");
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();

            sb.AppendLine("---");
            sb.AppendLine("_Report generated by ShadowStrike Platform — CONFIDENTIAL_");

            return sb.ToString();
        }
    }
}
