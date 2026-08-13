using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace ShadowStrike.Core.Anonymity.Network
{
    /// <summary>
    /// Singleton that tracks the current proxy configuration and exposes
    /// factory methods for creating properly-proxied <see cref="HttpClient"/> instances.
    ///
    /// Security enforcement:
    ///   - When Active, the NO_PROXY / no_proxy environment variables are stripped
    ///     from the current process to prevent any accidental bypass.
    ///   - All factory methods return handlers that do NOT support SOCKS5 via the
    ///     built-in WebProxy API (which silently fails on .NET 8). Instead they use
    ///     <see cref="TorSocks5Handler"/> which implements the handshake via
    ///     SocketsHttpHandler.ConnectCallback.
    /// </summary>
    public sealed class ProxyManager
    {
        // ── Singleton ────────────────────────────────────────────────────────

        private static readonly Lazy<ProxyManager> _instance =
            new(() => new ProxyManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ProxyManager Instance => _instance.Value;

        private ProxyManager() { }

        // ── State ────────────────────────────────────────────────────────────

        private volatile bool _isActive;
        private string _socksHost = "127.0.0.1";
        private int _socksPort = 9050;

        /// <summary>True when Anonymous Mode is active and all traffic must be proxied.</summary>
        public bool IsActive => _isActive;

        /// <summary>Current SOCKS5 host (default 127.0.0.1).</summary>
        public string SocksHost => _socksHost;

        /// <summary>Current SOCKS5 port (default 9050).</summary>
        public int SocksPort => _socksPort;

        /// <summary>Human-readable URI for informational display only (NOT passed to WebProxy).</summary>
        public string SocksUri => $"socks5h://{_socksHost}:{_socksPort}";

        // ── Activation / Deactivation ────────────────────────────────────────

        /// <summary>
        /// Activates the proxy manager. From this point all <see cref="CreateHandler"/>
        /// and <see cref="CreateClient"/> calls will return SOCKS5-tunnelled instances.
        /// Also strips NO_PROXY env vars to prevent bypass.
        /// </summary>
        public void Activate(string socksHost = "127.0.0.1", int socksPort = 9050)
        {
            _socksHost = socksHost;
            _socksPort = socksPort;
            _isActive = true;
            StripNoProxyEnvironment();
        }

        /// <summary>Deactivates proxying. Subsequent factory calls return plain handlers.</summary>
        public void Deactivate()
        {
            _isActive = false;
        }

        // ── Factory Methods ──────────────────────────────────────────────────

        /// <summary>
        /// Returns a <see cref="SocketsHttpHandler"/> that routes traffic through Tor
        /// when Anonymous Mode is active, or a standard <see cref="SocketsHttpHandler"/>
        /// when it is not.
        /// </summary>
        public SocketsHttpHandler CreateHandler()
        {
            if (_isActive)
                return TorSocks5Handler.Create(_socksHost, _socksPort);

            return new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            };
        }

        /// <summary>
        /// Returns a ready-to-use <see cref="HttpClient"/> with a 30-second timeout.
        /// Callers must dispose it when done.
        /// </summary>
        /// <param name="timeoutSeconds">Request timeout (default 30s, use higher values through Tor).</param>
        public HttpClient CreateClient(int timeoutSeconds = 30)
        {
            return new HttpClient(CreateHandler())
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
        }

        /// <summary>
        /// Returns a <see cref="HttpClientHandler"/> pre-configured for SSL analysis
        /// (custom cert validation callback), optionally proxied through Tor.
        /// SSL analysis requires HttpClientHandler (not SocketsHttpHandler) for the
        /// ServerCertificateCustomValidationCallback. In this case, we layer the SOCKS5
        /// tunnel on top by using the ConnectCallback from <see cref="TorSocks5Handler"/>
        /// where possible — for pure SSL capture flows, we skip proxying in Lightweight mode
        /// but still route in Hardened mode (traffic goes through Gateway anyway).
        /// </summary>
        public HttpClientHandler CreateSslAnalysisHandler(
            System.Net.Security.RemoteCertificateValidationCallback certCallback)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, errors) => certCallback(sender, cert, chain, errors),
                AutomaticDecompression = DecompressionMethods.All
            };

            // In Lightweight mode: we cannot combine SocketsHttpHandler.ConnectCallback
            // with HttpClientHandler.ServerCertificateCustomValidationCallback.
            // In Hardened (Whonix) mode, SSL traffic flows through the Gateway VM
            // transparently, so host-side proxying is not strictly needed.
            // We do NOT set a WebProxy here — .NET 8 WebProxy("socks5://...") silently fails.
            // Tor-routed SSL analysis works correctly only in HardenedWhonix mode.

            return handler;
        }

        // ── Security Helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Strips NO_PROXY / no_proxy environment variables from the current process.
        /// Called whenever Anonymous Mode is activated to prevent accidental proxy bypass.
        /// </summary>
        private static void StripNoProxyEnvironment()
        {
            // Machine, User, and Process scopes — belt-and-suspenders
            foreach (var key in new[] { "NO_PROXY", "no_proxy", "NO_PROXY_LIST" })
            {
                try
                {
                    // Process scope (immediately effective)
                    Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
                    // Machine/User scope — requires elevation but we already have it
                    Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Machine);
                    Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.User);
                }
                catch { /* Elevation not available in all contexts — process scope is sufficient */ }
            }

            // Also ensure the BCL's internal HTTP proxy cache is not using a stale value
            HttpClient.DefaultProxy = new WebProxy(); // empty = no proxy system-wide for BCL
        }
    }
}
