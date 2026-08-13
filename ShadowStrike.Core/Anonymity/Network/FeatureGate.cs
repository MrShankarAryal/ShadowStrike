using System;

namespace ShadowStrike.Core.Anonymity.Network
{
    /// <summary>
    /// Describes how a network feature behaves when Anonymous Mode is active.
    /// </summary>
    public enum AnonymousRestriction
    {
        /// <summary>Feature is fully available.</summary>
        Allowed,

        /// <summary>Feature is allowed but produces a user warning (e.g. large downloads).</summary>
        Throttled,

        /// <summary>Feature is completely disabled while Anonymous Mode is on.</summary>
        Blocked
    }

    /// <summary>
    /// Centralised gate for features that are incompatible with Tor or that
    /// would cause leaks when Anonymous Mode is active.
    ///
    /// All feature-restricted code paths must call <see cref="Check"/> before proceeding.
    /// The gate is a no-op when Anonymous Mode is inactive.
    /// </summary>
    public static class FeatureGate
    {
        // ── Feature restrictions table ───────────────────────────────────────

        /// <summary>
        /// Tor does not support UDP. Any module that sends UDP packets (UdpFlooder,
        /// AmplificationScanner) must be blocked in Anonymous Mode.
        /// </summary>
        public static AnonymousRestriction UDP => AnonymousRestriction.Blocked;

        /// <summary>
        /// ICMP (ping/traceroute) is not tunnelled through Tor. Block to prevent leaks.
        /// </summary>
        public static AnonymousRestriction ICMP => AnonymousRestriction.Blocked;

        /// <summary>
        /// WebRTC is a well-known anonymity killer — it can expose the real IP
        /// even behind a proxy. Block entirely in Anonymous Mode.
        /// </summary>
        public static AnonymousRestriction WebRTC => AnonymousRestriction.Blocked;

        /// <summary>
        /// Downloads larger than 50MB are throttled and the user is warned —
        /// large transfers through Tor are slow and may deanonymise via timing analysis.
        /// </summary>
        public static AnonymousRestriction LargeDownload => AnonymousRestriction.Throttled;

        /// <summary>File uploads via HTTPS are permitted through Tor.</summary>
        public static AnonymousRestriction FileUpload => AnonymousRestriction.Allowed;

        /// <summary>
        /// Third-party CDN connections that cannot be proxied must be blocked
        /// (they would bypass the Tor circuit and leak the real IP).
        /// </summary>
        public static AnonymousRestriction ThirdPartyCDN => AnonymousRestriction.Blocked;

        /// <summary>
        /// SYN flooding uses raw TCP sockets — acceptable through Tor TCP relay.
        /// </summary>
        public static AnonymousRestriction SynFlood => AnonymousRestriction.Allowed;

        // ── Gate check API ───────────────────────────────────────────────────

        /// <summary>
        /// Checks whether a named feature is permitted in the current mode.
        /// </summary>
        /// <param name="featureName">
        /// One of: UDP, ICMP, WebRTC, LargeDownload, FileUpload, ThirdPartyCDN, SynFlood
        /// </param>
        /// <param name="reason">Human-readable explanation when the feature is restricted.</param>
        /// <returns>True if the feature may proceed; false if it should be stopped.</returns>
        public static bool Check(string featureName, out string reason)
        {
            // If Anonymous Mode is not active, everything is permitted.
            if (!ProxyManager.Instance.IsActive)
            {
                reason = string.Empty;
                return true;
            }

            var restriction = featureName.ToUpperInvariant() switch
            {
                "UDP"          => UDP,
                "ICMP"         => ICMP,
                "WEBRTC"       => WebRTC,
                "LARGEDOWNLOAD" => LargeDownload,
                "FILEUPLOAD"   => FileUpload,
                "THIRDPARTYCDN" => ThirdPartyCDN,
                "SYNFLOOD"     => SynFlood,
                _              => AnonymousRestriction.Allowed
            };

            reason = restriction switch
            {
                AnonymousRestriction.Blocked =>
                    $"'{featureName}' is BLOCKED in Anonymous Mode — it cannot be tunnelled through Tor " +
                    $"and would leak your real IP address.",
                AnonymousRestriction.Throttled =>
                    $"'{featureName}' is THROTTLED in Anonymous Mode — large transfers through Tor are " +
                    $"slow and may reveal timing information. Proceed with caution.",
                _ => string.Empty
            };

            return restriction != AnonymousRestriction.Blocked;
        }

        /// <summary>
        /// Throws <see cref="AnonymousFeatureBlockedException"/> if the feature is Blocked.
        /// Use this in code paths where a feature must halt immediately.
        /// </summary>
        public static void Enforce(string featureName)
        {
            if (!Check(featureName, out var reason))
                throw new AnonymousFeatureBlockedException(featureName, reason);
        }

        /// <summary>Returns a display summary of all restrictions for the UI.</summary>
        public static string[] GetBlockedFeatureList() =>
            new[]
            {
                "UDP traffic (Tor is TCP-only)",
                "ICMP / Ping / Traceroute",
                "WebRTC (real-IP leak vector)",
                "Third-party CDN direct connections"
            };

        /// <summary>Returns a display summary of all throttled features for the UI.</summary>
        public static string[] GetThrottledFeatureList() =>
            new[] { "Downloads > 50 MB (timing risk)" };
    }

    /// <summary>Thrown when a blocked feature is used while Anonymous Mode is active.</summary>
    public sealed class AnonymousFeatureBlockedException : InvalidOperationException
    {
        public string FeatureName { get; }

        public AnonymousFeatureBlockedException(string featureName, string reason)
            : base(reason)
        {
            FeatureName = featureName;
        }
    }
}
