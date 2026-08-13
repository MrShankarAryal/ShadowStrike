using System.Net.Http;

namespace ShadowStrike.Core.Anonymity.Network
{
    /// <summary>
    /// Centralised factory for <see cref="HttpClient"/> instances that are
    /// automatically routed through Tor when Anonymous Mode is active.
    ///
    /// All network-making modules in ShadowStrike.Core MUST obtain their
    /// <see cref="HttpClient"/> from this factory so that the proxy guarantee holds.
    /// Direct <c>new HttpClient()</c> or <c>new HttpClientHandler()</c> calls
    /// are prohibited when Anonymous Mode may be active.
    /// </summary>
    public static class AnonymousHttpClientFactory
    {
        /// <summary>
        /// Creates a general-purpose <see cref="HttpClient"/>.
        /// When <see cref="ProxyManager.IsActive"/> is true, traffic is tunnelled
        /// through Tor via the custom SOCKS5 ConnectCallback handler.
        /// </summary>
        /// <param name="timeoutSeconds">
        /// Default 30s. Tor circuits are slower — callers performing scans should
        /// pass 60 or more.
        /// </param>
        public static HttpClient Create(int timeoutSeconds = 30) =>
            ProxyManager.Instance.CreateClient(timeoutSeconds);

        /// <summary>
        /// Creates a <see cref="SocketsHttpHandler"/> suitable for use with
        /// <c>new HttpClient(handler)</c> when the caller needs to configure
        /// additional handler options after creation.
        /// </summary>
        public static SocketsHttpHandler CreateHandler() =>
            ProxyManager.Instance.CreateHandler();

        /// <summary>
        /// Creates an <see cref="HttpClient"/> for SSL certificate analysis.
        /// Accepts a custom certificate validation callback.
        /// In Hardened (Whonix) mode traffic is transparently Tor-routed at the VM layer.
        /// </summary>
        public static HttpClient CreateForSslAnalysis(
            System.Net.Security.RemoteCertificateValidationCallback certCallback,
            int timeoutSeconds = 15)
        {
            var handler = ProxyManager.Instance.CreateSslAnalysisHandler(certCallback);
            return new HttpClient(handler) { Timeout = System.TimeSpan.FromSeconds(timeoutSeconds) };
        }
    }
}
