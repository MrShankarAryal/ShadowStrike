using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using ShadowStrike.Core.Platform.Audit;

namespace ShadowStrike.Core.Platform.Verification
{
    /// <summary>Statistical result for one verification comparison.</summary>
    public sealed class VerificationResult
    {
        public string  Label           { get; init; } = string.Empty;
        public double  PValue          { get; init; }
        public double  TStatistic      { get; init; }
        public double  Df              { get; init; }
        public double  MeanA           { get; init; }
        public double  MeanB           { get; init; }
        public bool    IsSignificant   { get; init; }
        public string  Interpretation  { get; init; } = string.Empty;

        // Attached proof artifact (e.g., raw samples, histogram bins, confidence interval).
        public VerificationProof? Proof { get; init; }
    }

    /// <summary>
    /// A serialisable proof bundle attached to each verification result.
    /// Stored in the engagement report so findings are reproducible.
    /// </summary>
    public sealed class VerificationProof
    {
        public double[]   SamplesA  { get; init; } = Array.Empty<double>();
        public double[]   SamplesB  { get; init; } = Array.Empty<double>();
        public double[]   CiLower   { get; init; } = Array.Empty<double>(); // bootstrap 95% CI lower
        public double[]   CiUpper   { get; init; } = Array.Empty<double>(); // bootstrap 95% CI upper
        public DateTime   Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Verification engine for timing-based attack detection.
    /// Uses Welch's two-sample t-test (unequal variances) on response-time
    /// distributions — NOT a simple threshold — to produce statistically
    /// valid, reproducible evidence of injection-based timing differences.
    /// </summary>
    public sealed class VerificationEngine
    {
        // ── Singleton ────────────────────────────────────────────────────────
        private static VerificationEngine? _instance;
        private static readonly object _lock = new();

        public static VerificationEngine Instance
        {
            get
            {
                if (_instance is null)
                    lock (_lock)
                        _instance ??= new VerificationEngine();
                return _instance;
            }
        }

        // ── Configuration ─────────────────────────────────────────────────────
        /// <summary>Significance threshold (α). Default 0.05 (95% confidence).</summary>
        public double AlphaLevel { get; set; } = 0.05;

        /// <summary>Minimum samples required in each group before running the test.</summary>
        public int MinSamples { get; set; } = 5;

        // ── Core Test ─────────────────────────────────────────────────────────

        /// <summary>
        /// Run Welch's t-test comparing two independent response-time samples.
        /// </summary>
        /// <param name="label">Human-readable description of what is being tested.</param>
        /// <param name="baselineSamples">Response times (ms) for the benign/baseline payload.</param>
        /// <param name="injectedSamples">Response times (ms) for the injected payload.</param>
        /// <param name="attachProof">Whether to embed raw samples in the proof artifact.</param>
        public VerificationResult RunWelchTest(
            string             label,
            IList<double>      baselineSamples,
            IList<double>      injectedSamples,
            bool               attachProof = true)
        {
            if (baselineSamples.Count < MinSamples || injectedSamples.Count < MinSamples)
                throw new ArgumentException(
                    $"Each group needs at least {MinSamples} samples. " +
                    $"Got baseline={baselineSamples.Count}, injected={injectedSamples.Count}.");

            double[] a = baselineSamples.ToArray();
            double[] b = injectedSamples.ToArray();

            double meanA  = Statistics.Mean(a);
            double meanB  = Statistics.Mean(b);
            double varA   = Statistics.Variance(a);
            double varB   = Statistics.Variance(b);
            int    nA     = a.Length;
            int    nB     = b.Length;

            // Welch's t-statistic.
            double tStat  = (meanA - meanB) / Math.Sqrt(varA / nA + varB / nB);

            // Welch-Satterthwaite degrees of freedom.
            double numerator   = Math.Pow(varA / nA + varB / nB, 2);
            double denominator = Math.Pow(varA / nA, 2) / (nA - 1) + Math.Pow(varB / nB, 2) / (nB - 1);
            double df          = numerator / denominator;

            // Two-tailed p-value using the Student T CDF.
            double pValue = 2.0 * StudentT.CDF(0, 1, df, -Math.Abs(tStat));

            bool   isSignificant = pValue < AlphaLevel;
            string interpretation = BuildInterpretation(label, meanA, meanB, tStat, df, pValue, isSignificant);

            VerificationProof? proof = null;
            if (attachProof)
            {
                (double[] lower, double[] upper) = BootstrapConfidenceInterval(a, b, iterations: 1000);
                proof = new VerificationProof
                {
                    SamplesA  = a,
                    SamplesB  = b,
                    CiLower   = lower,
                    CiUpper   = upper,
                    Timestamp = DateTime.UtcNow
                };
            }

            var result = new VerificationResult
            {
                Label          = label,
                PValue         = pValue,
                TStatistic     = tStat,
                Df             = df,
                MeanA          = meanA,
                MeanB          = meanB,
                IsSignificant  = isSignificant,
                Interpretation = interpretation,
                Proof          = proof
            };

            AuditLogger.Instance.Log(AuditEventType.VerificationResult,
                $"{label}: t={tStat:F4} df={df:F2} p={pValue:F6} α={AlphaLevel} " +
                $"significant={isSignificant} meanBaseline={meanA:F2}ms meanInjected={meanB:F2}ms");

            return result;
        }

        /// <summary>
        /// Convenience overload: detect time-based blind SQLi by comparing baseline
        /// response times to those triggered by a sleep/delay injection.
        /// </summary>
        public VerificationResult DetectTimingInjection(
            string        label,
            double[]      baselineMs,
            double[]      injectedMs)
            => RunWelchTest(label, baselineMs, injectedMs, attachProof: true);

        // ── Bootstrap CI ─────────────────────────────────────────────────────

        /// <summary>
        /// Compute a non-parametric bootstrap 95 % confidence interval for
        /// the difference of means (meanB - meanA) using <paramref name="iterations"/> resamples.
        /// Returns (lower[0], upper[0]) — single-element arrays for JSON-serialisation symmetry.
        /// </summary>
        private static (double[] lower, double[] upper) BootstrapConfidenceInterval(
            double[] a, double[] b, int iterations = 1000)
        {
            var rng  = new Random();
            var diffs = new double[iterations];

            for (int i = 0; i < iterations; i++)
            {
                double mA = ResampleMean(a, rng);
                double mB = ResampleMean(b, rng);
                diffs[i]  = mB - mA;
            }

            Array.Sort(diffs);
            double lower = diffs[(int)(0.025 * iterations)];
            double upper = diffs[(int)(0.975 * iterations)];
            return (new[] { lower }, new[] { upper });
        }

        private static double ResampleMean(double[] data, Random rng)
        {
            double sum = 0;
            for (int i = 0; i < data.Length; i++)
                sum += data[rng.Next(data.Length)];
            return sum / data.Length;
        }

        // ── Interpretation Builder ────────────────────────────────────────────

        private static string BuildInterpretation(
            string label,
            double meanA, double meanB,
            double t, double df, double p,
            bool   significant)
        {
            double diff    = meanB - meanA;
            string direction = diff > 0 ? "slower" : "faster";

            if (significant)
                return $"[CONFIRMED] '{label}': The injected payload produced responses " +
                       $"{Math.Abs(diff):F2} ms {direction} than baseline " +
                       $"(t={t:F4}, df={df:F1}, p={p:F6} < α). " +
                       "This is statistically significant evidence of a timing-based vulnerability.";
            else
                return $"[NOT CONFIRMED] '{label}': No statistically significant timing difference " +
                       $"detected (t={t:F4}, df={df:F1}, p={p:F6} ≥ α). " +
                       "Cannot conclude a timing-based vulnerability from these samples.";
        }
    }
}
