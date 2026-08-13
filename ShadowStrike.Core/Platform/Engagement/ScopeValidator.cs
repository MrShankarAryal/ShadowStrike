using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using NSec.Cryptography;
using ShadowStrike.Core.Platform.Audit;

namespace ShadowStrike.Core.Platform.Engagement
{
    /// <summary>
    /// The single choke-point for every network request and attack technique.
    /// All modes MUST call IsAllowed() before any I/O or file mutation.
    /// </summary>
    public sealed class ScopeValidator
    {
        // ── Singleton ────────────────────────────────────────────────────────
        private static ScopeValidator? _instance;
        private static readonly object _lock = new();

        public static ScopeValidator Instance
        {
            get
            {
                if (_instance is null)
                    lock (_lock)
                        _instance ??= new ScopeValidator();
                return _instance;
            }
        }

        // ── State ────────────────────────────────────────────────────────────
        private EngagementPolicy? _policy;
        private bool _signatureVerified = false;

        /// <summary>The loaded engagement policy (null until LoadPolicy() is called).</summary>
        public EngagementPolicy? Policy => _policy;

        /// <summary>True only after a policy has been loaded AND its Ed25519 signature verified.</summary>
        public bool IsLoaded => _policy is not null && _signatureVerified;

        // Ed25519 public key used to verify engagement files.
        // Replace this 32-byte hex string with the real operator public key at deployment time.
        private static readonly byte[] _operatorPublicKeyBytes = Convert.FromHexString(
            "0000000000000000000000000000000000000000000000000000000000000000");

        // ── Load ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Load an engagement JSON file from disk, validate its schema, and verify the
        /// Ed25519 signature.  Throws <see cref="InvalidOperationException"/> on failure.
        /// </summary>
        public void LoadPolicy(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Engagement file not found: {jsonPath}");

            string raw = File.ReadAllText(jsonPath);
            EngagementPolicy policy = JsonSerializer.Deserialize<EngagementPolicy>(raw)
                ?? throw new InvalidOperationException("Engagement JSON deserialized to null.");

            ValidateSchema(policy);
            VerifySignature(policy, raw);

            lock (_lock)
            {
                _policy = policy;
                _signatureVerified = true;
            }

            AuditLogger.Instance.Log(AuditEventType.EngagementLoaded,
                $"Engagement '{policy.Engagement}' loaded for client '{policy.Client}' " +
                $"by operator '{policy.Operator}'. Valid {policy.ValidFrom:u} – {policy.ValidUntil:u}.");
        }

        // ── IsAllowed — THE CHOKE POINT ──────────────────────────────────────

        /// <summary>
        /// Determines whether a request to <paramref name="target"/> using
        /// <paramref name="mode"/> and <paramref name="technique"/> is allowed by
        /// the current engagement policy.
        /// </summary>
        /// <param name="target">Hostname, IP, or URL of the target.</param>
        /// <param name="mode">Attack mode name (e.g., "sqli", "xss", "recon").</param>
        /// <param name="technique">Specific technique within the mode (e.g., "union", "stored").</param>
        /// <returns><c>true</c> when allowed; <c>false</c> when blocked.</returns>
        public bool IsAllowed(string target, string mode, string technique)
        {
            // 1. Fail-closed: no policy loaded → block everything.
            if (!IsLoaded || _policy is null)
            {
                AuditLogger.Instance.Log(AuditEventType.ScopeViolation,
                    $"BLOCKED (no policy): target={target} mode={mode} technique={technique}");
                return false;
            }

            // 2. Temporal check.
            DateTime now = DateTime.UtcNow;
            if (now < _policy.ValidFrom || now > _policy.ValidUntil)
            {
                AuditLogger.Instance.Log(AuditEventType.ScopeViolation,
                    $"BLOCKED (expired engagement): target={target} mode={mode} technique={technique} " +
                    $"now={now:u} valid=[{_policy.ValidFrom:u},{_policy.ValidUntil:u}]");
                return false;
            }

            // 3. Mode check.
            if (!_policy.AllowedModes.Contains(mode.ToLowerInvariant()))
            {
                AuditLogger.Instance.Log(AuditEventType.ScopeViolation,
                    $"BLOCKED (mode not allowed): target={target} mode={mode} technique={technique}");
                return false;
            }

            // 4. Target check.
            TargetScope? scope = FindScope(target);
            if (scope is null)
            {
                AuditLogger.Instance.Log(AuditEventType.ScopeViolation,
                    $"BLOCKED (out-of-scope): target={target} mode={mode} technique={technique}");
                return false;
            }

            // 5. Destructive-test gate.
            if (scope.Destructive == false && IsDestructiveTechnique(technique))
            {
                AuditLogger.Instance.Log(AuditEventType.ScopeViolation,
                    $"BLOCKED (destructive technique on non-destructive target): " +
                    $"target={target} mode={mode} technique={technique}");
                return false;
            }

            // 6. Global destructive-tests gate.
            if (_policy.DestructiveTests == false && IsDestructiveTechnique(technique))
            {
                AuditLogger.Instance.Log(AuditEventType.ScopeViolation,
                    $"BLOCKED (destructive tests globally disabled): " +
                    $"target={target} mode={mode} technique={technique}");
                return false;
            }

            // Allowed — record access.
            AuditLogger.Instance.Log(AuditEventType.ScopeAllowed,
                $"ALLOWED: target={target} mode={mode} technique={technique}");
            return true;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private TargetScope? FindScope(string target)
        {
            if (_policy is null) return null;

            string normalizedTarget = NormalizeHost(target);

            foreach (TargetScope scope in _policy.Targets)
            {
                string normalizedScope = NormalizeHost(scope.Host);

                // Exact match.
                if (string.Equals(normalizedScope, normalizedTarget, StringComparison.OrdinalIgnoreCase))
                    return scope;

                // Wildcard subdomain: *.example.com
                if (normalizedScope.StartsWith("*.", StringComparison.Ordinal))
                {
                    string suffix = normalizedScope[2..]; // e.g., "example.com"
                    if (normalizedTarget.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(normalizedTarget, suffix, StringComparison.OrdinalIgnoreCase))
                        return scope;
                }

                // CIDR range.
                if (TryParseCidr(normalizedScope, out IPAddress? network, out int prefix) && network is not null)
                {
                    if (IPAddress.TryParse(normalizedTarget, out IPAddress? targetIp) && targetIp is not null)
                    {
                        if (IsInCidr(targetIp, network, prefix))
                            return scope;
                    }
                }
            }

            return null;
        }

        private static string NormalizeHost(string host)
        {
            // Strip scheme and path if a full URL is passed.
            if (Uri.TryCreate(host, UriKind.Absolute, out Uri? uri))
                return uri.Host.ToLowerInvariant();
            return host.ToLowerInvariant().TrimEnd('/');
        }

        private static readonly HashSet<string> _destructiveTechniques = new(StringComparer.OrdinalIgnoreCase)
        {
            "drop", "truncate", "delete", "format", "wipe", "overwrite",
            "ransomware", "encrypt", "destroy", "rm", "rmdir"
        };

        private static bool IsDestructiveTechnique(string technique) =>
            _destructiveTechniques.Contains(technique);

        // ── Ed25519 Signature Verification ──────────────────────────────────

        private static void VerifySignature(EngagementPolicy policy, string rawJson)
        {
            // If the operator public key is all-zeros (development default), skip verification.
            bool allZero = true;
            foreach (byte b in _operatorPublicKeyBytes) { if (b != 0) { allZero = false; break; } }
            if (allZero)
            {
                AuditLogger.Instance.Log(AuditEventType.SignatureVerification,
                    "WARNING: Ed25519 verification skipped — operator public key is the zero key (development mode).");
                return;
            }

            if (string.IsNullOrWhiteSpace(policy.Signature))
                throw new InvalidOperationException("Engagement file has no signature field.");

            byte[] sigBytes;
            try { sigBytes = Convert.FromBase64String(policy.Signature); }
            catch (FormatException) { throw new InvalidOperationException("Signature is not valid Base64."); }

            // Build the message: JSON with the signature field replaced by empty string.
            string messageJson = RemoveSignatureField(rawJson);
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);

            var algorithm = SignatureAlgorithm.Ed25519;
            PublicKey publicKey;
            try
            {
                publicKey = PublicKey.Import(algorithm, _operatorPublicKeyBytes, KeyBlobFormat.RawPublicKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to import operator public key: {ex.Message}", ex);
            }

            bool valid = algorithm.Verify(publicKey, messageBytes, sigBytes);
            if (!valid)
                throw new InvalidOperationException("Engagement file Ed25519 signature verification FAILED. File may be tampered.");

            AuditLogger.Instance.Log(AuditEventType.SignatureVerification,
                "Ed25519 signature verified successfully.");
        }

        private static string RemoveSignatureField(string rawJson)
        {
            // Remove the "signature" key/value for canonical message construction.
            using JsonDocument doc = JsonDocument.Parse(rawJson);
            var dict = new Dictionary<string, object?>();
            foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
            {
                if (!prop.Name.Equals("signature", StringComparison.OrdinalIgnoreCase))
                    dict[prop.Name] = prop.Value.Clone();
            }
            return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = false });
        }

        // ── Schema Validation ────────────────────────────────────────────────

        private static void ValidateSchema(EngagementPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(policy.Engagement))
                throw new InvalidOperationException("Engagement field is required.");
            if (string.IsNullOrWhiteSpace(policy.Client))
                throw new InvalidOperationException("Client field is required.");
            if (string.IsNullOrWhiteSpace(policy.Operator))
                throw new InvalidOperationException("Operator field is required.");
            if (policy.ValidFrom == default || policy.ValidUntil == default)
                throw new InvalidOperationException("ValidFrom and ValidUntil are required.");
            if (policy.ValidFrom >= policy.ValidUntil)
                throw new InvalidOperationException("ValidFrom must be before ValidUntil.");
            if (policy.Targets == null || policy.Targets.Count == 0)
                throw new InvalidOperationException("At least one target is required.");
            if (policy.AllowedModes == null || policy.AllowedModes.Count == 0)
                throw new InvalidOperationException("At least one allowed mode is required.");
        }

        // ── CIDR Utilities ───────────────────────────────────────────────────

        private static bool TryParseCidr(string cidr, out IPAddress? network, out int prefix)
        {
            network = null; prefix = 0;
            int slash = cidr.IndexOf('/');
            if (slash < 0) return false;
            if (!IPAddress.TryParse(cidr[..slash], out network)) return false;
            return int.TryParse(cidr[(slash + 1)..], out prefix);
        }

        private static bool IsInCidr(IPAddress target, IPAddress network, int prefix)
        {
            byte[] targetBytes = target.GetAddressBytes();
            byte[] networkBytes = network.GetAddressBytes();
            if (targetBytes.Length != networkBytes.Length) return false;

            int fullBytes = prefix / 8;
            int remainBits = prefix % 8;

            for (int i = 0; i < fullBytes; i++)
                if (targetBytes[i] != networkBytes[i]) return false;

            if (remainBits > 0)
            {
                byte mask = (byte)(0xFF << (8 - remainBits));
                if ((targetBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask)) return false;
            }
            return true;
        }
    }
}
