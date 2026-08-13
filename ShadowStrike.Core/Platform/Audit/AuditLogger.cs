using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace ShadowStrike.Core.Platform.Audit
{
    // ── Event taxonomy ───────────────────────────────────────────────────────
    public enum AuditEventType
    {
        EngagementLoaded,
        SignatureVerification,
        ScopeAllowed,
        ScopeViolation,
        VerificationResult,
        RollbackRegistered,
        RollbackExecuted,
        RollbackFailed,
        AttackStarted,
        AttackCompleted,
        AttackFailed,
        ReportGenerated,
        SystemInfo,
        Warning,
        Error
    }

    // ── Immutable audit record ───────────────────────────────────────────────
    public sealed class AuditEvent
    {
        [JsonPropertyName("seq")]         public long   Seq         { get; init; }
        [JsonPropertyName("timestamp")]   public string Timestamp   { get; init; } = string.Empty;
        [JsonPropertyName("type")]        public string Type        { get; init; } = string.Empty;
        [JsonPropertyName("message")]     public string Message     { get; init; } = string.Empty;
        [JsonPropertyName("threadId")]    public int    ThreadId    { get; init; }
        [JsonPropertyName("hmac")]        public string Hmac        { get; init; } = string.Empty;
        [JsonPropertyName("prevHmac")]    public string PrevHmac    { get; init; } = string.Empty;
    }

    /// <summary>
    /// Thread-safe audit logger with HMAC-SHA256 hash-chaining for tamper-evidence.
    /// Writes JSON-Lines to %APPDATA%\ShadowStrike\audit\audit-&lt;date&gt;.jsonl
    /// and keeps an in-memory ring buffer for the report generator.
    /// </summary>
    public sealed class AuditLogger
    {
        // ── Singleton ────────────────────────────────────────────────────────
        private static AuditLogger? _instance;
        private static readonly object _instanceLock = new();

        public static AuditLogger Instance
        {
            get
            {
                if (_instance is null)
                    lock (_instanceLock)
                        _instance ??= new AuditLogger();
                return _instance;
            }
        }

        // ── State ────────────────────────────────────────────────────────────
        private readonly string _logPath;
        private readonly StreamWriter _writer;
        private readonly object _writeLock = new();
        private readonly ConcurrentQueue<AuditEvent> _buffer = new();
        private long _seq = 0;
        private string _prevHmac = "GENESIS";
        private readonly byte[] _hmacKey;

        // ── Construction ─────────────────────────────────────────────────────
        private AuditLogger()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ShadowStrike", "audit");
            Directory.CreateDirectory(dir);

            string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            _logPath = Path.Combine(dir, $"audit-{dateStr}.jsonl");

            // Derive a per-session HMAC key from a machine-bound value + random salt.
            _hmacKey = DeriveHmacKey();

            _writer = new StreamWriter(_logPath, append: true, Encoding.UTF8)
            {
                AutoFlush = true
            };

            Log(AuditEventType.SystemInfo, $"AuditLogger started. Session log: {_logPath}");
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Write an audit event synchronously.</summary>
        public void Log(AuditEventType type, string message)
        {
            long seq = Interlocked.Increment(ref _seq);

            string prevHmac;
            lock (_writeLock) prevHmac = _prevHmac;

            string timestamp = DateTime.UtcNow.ToString("o");
            string typeStr   = type.ToString();
            int    threadId  = Environment.CurrentManagedThreadId;

            // Compute HMAC over: seq|timestamp|type|message|prevHmac
            string payload = $"{seq}|{timestamp}|{typeStr}|{message}|{prevHmac}";
            string hmac    = ComputeHmac(payload);

            var evt = new AuditEvent
            {
                Seq       = seq,
                Timestamp = timestamp,
                Type      = typeStr,
                Message   = message,
                ThreadId  = threadId,
                Hmac      = hmac,
                PrevHmac  = prevHmac
            };

            string json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            lock (_writeLock)
            {
                _prevHmac = hmac;
                _writer.WriteLine(json);
            }

            _buffer.Enqueue(evt);

            // Keep buffer bounded (last 10 000 events).
            while (_buffer.Count > 10_000)
                _buffer.TryDequeue(out _);
        }

        /// <summary>Return all buffered events (copy).</summary>
        public IReadOnlyList<AuditEvent> GetBufferedEvents()
            => new List<AuditEvent>(_buffer);

        /// <summary>Verify the hash chain of the on-disk log. Returns (isValid, firstBrokenSeq).</summary>
        public (bool IsValid, long? FirstBrokenSeq) VerifyChain()
        {
            if (!File.Exists(_logPath)) return (true, null);

            string prev = "GENESIS";
            foreach (string line in File.ReadLines(_logPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                AuditEvent? evt;
                try { evt = JsonSerializer.Deserialize<AuditEvent>(line); }
                catch { return (false, -1); }
                if (evt is null) return (false, -1);

                if (evt.PrevHmac != prev) return (false, evt.Seq);

                string payload  = $"{evt.Seq}|{evt.Timestamp}|{evt.Type}|{evt.Message}|{evt.PrevHmac}";
                string expected = ComputeHmac(payload);
                if (evt.Hmac != expected) return (false, evt.Seq);

                prev = evt.Hmac;
            }
            return (true, null);
        }

        // ── HMAC helpers ─────────────────────────────────────────────────────

        private string ComputeHmac(string payload)
        {
            using var hmac = new HMACSHA256(_hmacKey);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash);
        }

        private static byte[] DeriveHmacKey()
        {
            // Base: machine name + user name (stable within a session).
            string seed = $"{Environment.MachineName}:{Environment.UserName}";
            // XOR with 16 random bytes to prevent cross-machine replay.
            byte[] random = RandomNumberGenerator.GetBytes(32);
            byte[] seedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
            for (int i = 0; i < 32; i++) seedBytes[i] ^= random[i];
            return seedBytes;
        }
    }
}
