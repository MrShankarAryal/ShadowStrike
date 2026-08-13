using ShadowStrike.Core.Anonymity.Network;
using Xunit;

namespace ShadowStrike.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ProxyManager"/>.
    /// Tests proxy activation/deactivation, NO_PROXY stripping, and handler creation.
    /// NOTE: These tests use reflection to reset the singleton between tests.
    /// </summary>
    [Collection("ProxyTests")]
    public class ProxyManagerTests
    {
        // Reset singleton state between tests by deactivating
        private void EnsureDeactivated() => ProxyManager.Instance.Deactivate();

        // ── IsActive state ────────────────────────────────────────────────────

        [Fact]
        public void IsActive_DefaultState_IsFalse()
        {
            EnsureDeactivated();
            Assert.False(ProxyManager.Instance.IsActive);
        }

        [Fact]
        public void Activate_SetsIsActiveTrue()
        {
            EnsureDeactivated();
            ProxyManager.Instance.Activate("127.0.0.1", 9050);
            Assert.True(ProxyManager.Instance.IsActive);
            EnsureDeactivated();
        }

        [Fact]
        public void Deactivate_SetsIsActiveFalse()
        {
            ProxyManager.Instance.Activate("127.0.0.1", 9050);
            ProxyManager.Instance.Deactivate();
            Assert.False(ProxyManager.Instance.IsActive);
        }

        // ── SocksUri property ─────────────────────────────────────────────────

        [Fact]
        public void SocksUri_ReflectsConfiguredValues()
        {
            EnsureDeactivated();
            ProxyManager.Instance.Activate("127.0.0.1", 9050);
            Assert.Equal("socks5h://127.0.0.1:9050", ProxyManager.Instance.SocksUri);
            EnsureDeactivated();
        }

        [Fact]
        public void Activate_CustomPort_ReflectedInUri()
        {
            EnsureDeactivated();
            ProxyManager.Instance.Activate("127.0.0.1", 19050);
            Assert.Equal("socks5h://127.0.0.1:19050", ProxyManager.Instance.SocksUri);
            EnsureDeactivated();
            // restore
            ProxyManager.Instance.Activate("127.0.0.1", 9050);
            ProxyManager.Instance.Deactivate();
        }

        // ── NO_PROXY stripping ────────────────────────────────────────────────

        [Fact]
        public void Activate_StripsNoProxyEnvironmentVariable()
        {
            System.Environment.SetEnvironmentVariable("NO_PROXY", "localhost,127.0.0.1",
                System.EnvironmentVariableTarget.Process);
            System.Environment.SetEnvironmentVariable("no_proxy", "localhost",
                System.EnvironmentVariableTarget.Process);

            ProxyManager.Instance.Activate("127.0.0.1", 9050);

            Assert.Null(System.Environment.GetEnvironmentVariable("NO_PROXY",
                System.EnvironmentVariableTarget.Process));
            Assert.Null(System.Environment.GetEnvironmentVariable("no_proxy",
                System.EnvironmentVariableTarget.Process));

            EnsureDeactivated();
        }

        // ── Handler creation ──────────────────────────────────────────────────

        [Fact]
        public void CreateHandler_WhenActive_ReturnsSocketsHttpHandler()
        {
            ProxyManager.Instance.Activate("127.0.0.1", 9050);
            var handler = ProxyManager.Instance.CreateHandler();
            Assert.IsType<System.Net.Http.SocketsHttpHandler>(handler);
            handler.Dispose();
            EnsureDeactivated();
        }

        [Fact]
        public void CreateHandler_WhenInactive_ReturnsSocketsHttpHandler()
        {
            EnsureDeactivated();
            var handler = ProxyManager.Instance.CreateHandler();
            Assert.IsType<System.Net.Http.SocketsHttpHandler>(handler);
            handler.Dispose();
        }

        [Fact]
        public void CreateHandler_WhenActive_UseProxyIsFalse()
        {
            // UseProxy = false is the correct setting when using ConnectCallback
            // (we ARE the proxy; built-in proxy logic must be disabled)
            ProxyManager.Instance.Activate("127.0.0.1", 9050);
            var handler = ProxyManager.Instance.CreateHandler();
            Assert.False(handler.UseProxy);
            handler.Dispose();
            EnsureDeactivated();
        }

        [Fact]
        public void CreateClient_ReturnsHttpClientWithCorrectTimeout()
        {
            EnsureDeactivated();
            using var client = ProxyManager.Instance.CreateClient(45);
            Assert.Equal(System.TimeSpan.FromSeconds(45), client.Timeout);
        }
    }
}
