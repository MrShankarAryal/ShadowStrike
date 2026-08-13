using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShadowStrike.Core.Anonymity
{
    /// <summary>
    /// Persisted anonymity engine settings stored at
    /// %APPDATA%\ShadowStrike\settings.json
    /// </summary>
    public class AnonymitySettings
    {
        private static readonly string _settingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShadowStrike");

        private static readonly string _settingsPath =
            Path.Combine(_settingsDir, "settings.json");

        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // ── General ──────────────────────────────────────────────────────────

        /// <summary>Last-used anonymity mode. LightweightTor is the default on first run.</summary>
        public AnonymityMode DefaultMode { get; set; } = AnonymityMode.LightweightTor;

        /// <summary>Whether to start Anonymous Mode automatically on app launch.</summary>
        public bool AutoStartAnonymousMode { get; set; } = false;

        // ── tor.exe path (LightweightTor mode) ───────────────────────────────

        /// <summary>
        /// Explicit path to tor.exe. Leave null/empty to use the auto-discovery order:
        ///   1. This setting value
        ///   2. PATH environment variable
        ///   3. %LOCALAPPDATA%\Tor Browser\Browser\TorBrowser\Tor\tor.exe
        ///   4. C:\Tor\tor.exe
        ///   5. C:\Program Files\Tor\tor.exe
        /// </summary>
        public string? TorExecutablePath { get; set; }

        /// <summary>SOCKS5 port for the lightweight tor.exe process.</summary>
        public int TorSocksPort { get; set; } = 9050;

        /// <summary>Control port for tor.exe NEWNYM identity rotation.</summary>
        public int TorControlPort { get; set; } = 9051;

        // ── Whonix VM (HardenedWhonix mode) ──────────────────────────────────

        /// <summary>
        /// Download mirror for Whonix OVA images. Override to use a custom/local mirror.
        /// Default: https://download.whonix.org
        /// </summary>
        public string WhonixMirrorUrl { get; set; } = "https://download.whonix.org";

        /// <summary>
        /// Local path to user-supplied Whonix-Gateway .ova file.
        /// Populated by the "I already have the .ova files" browse flow.
        /// </summary>
        public string? WhonixGatewayOvaPath { get; set; }

        /// <summary>
        /// Local path to user-supplied Whonix-Workstation .ova file.
        /// Populated by the "I already have the .ova files" browse flow.
        /// </summary>
        public string? WhonixWorkstationOvaPath { get; set; }

        /// <summary>Directory where downloaded Whonix OVA files are cached.</summary>
        public string WhonixDownloadCacheDir { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ShadowStrike", "WhonixCache");

        /// <summary>Name of the imported Whonix-Gateway VM in VirtualBox.</summary>
        public string WhonixGatewayVmName { get; set; } = "Whonix-Gateway";

        /// <summary>Name of the imported Whonix-Workstation VM in VirtualBox.</summary>
        public string WhonixWorkstationVmName { get; set; } = "Whonix-Workstation";

        /// <summary>Name of the VirtualBox internal network shared between the two VMs.</summary>
        public string WhonixInternalNetName { get; set; } = "Whonix";

        /// <summary>Clean-state snapshot name. Restored on every session end.</summary>
        public string CleanSnapshotName { get; set; } = "clean";

        /// <summary>Gateway Tor-bootstrap timeout in seconds (default 180).</summary>
        public int GatewayBootstrapTimeoutSeconds { get; set; } = 180;

        /// <summary>
        /// Host port used for the VirtualBox natpf1 rule that exposes the
        /// Gateway's Tor SOCKS5 port to the host. Matches the Gateway's internal 9050.
        /// </summary>
        public int TorGatewayHostPort { get; set; } = 9050;

        // ── Persistence ───────────────────────────────────────────────────────

        public static AnonymitySettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<AnonymitySettings>(json, _jsonOptions)
                           ?? new AnonymitySettings();
                }
            }
            catch { /* Fall through — return defaults */ }
            return new AnonymitySettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(_settingsDir);
                var json = JsonSerializer.Serialize(this, _jsonOptions);
                File.WriteAllText(_settingsPath, json);
            }
            catch { /* Best-effort — non-fatal */ }
        }
    }
}
