using ShadowStrike.Core.Anonymity.Network;
using Xunit;

namespace ShadowStrike.Tests
{
    /// <summary>
    /// Unit tests for <see cref="FeatureGate"/>.
    /// Validates feature restrictions in both Anonymous Mode active and inactive states.
    /// </summary>
    [Collection("ProxyTests")]
    public class FeatureGateTests
    {
        private void EnsureDeactivated() => ProxyManager.Instance.Deactivate();

        [Fact]
        public void Check_WhenAnonymousModeInactive_AllowsAllFeatures()
        {
            EnsureDeactivated();

            Assert.True(FeatureGate.Check("UDP", out var reasonUdp));
            Assert.Empty(reasonUdp);

            Assert.True(FeatureGate.Check("ICMP", out var reasonIcmp));
            Assert.Empty(reasonIcmp);

            Assert.True(FeatureGate.Check("WebRTC", out var reasonWebRtc));
            Assert.Empty(reasonWebRtc);

            Assert.True(FeatureGate.Check("LargeDownload", out var reasonDownload));
            Assert.Empty(reasonDownload);

            Assert.True(FeatureGate.Check("FileUpload", out var reasonUpload));
            Assert.Empty(reasonUpload);
        }

        [Fact]
        public void Check_WhenAnonymousModeActive_BlocksUdpIcmpWebRtc()
        {
            EnsureDeactivated();
            ProxyManager.Instance.Activate("127.0.0.1", 9050);

            Assert.False(FeatureGate.Check("UDP", out var reasonUdp));
            Assert.Contains("BLOCKED", reasonUdp);

            Assert.False(FeatureGate.Check("ICMP", out var reasonIcmp));
            Assert.Contains("BLOCKED", reasonIcmp);

            Assert.False(FeatureGate.Check("WebRTC", out var reasonWebRtc));
            Assert.Contains("BLOCKED", reasonWebRtc);

            EnsureDeactivated();
        }

        [Fact]
        public void Check_WhenAnonymousModeActive_ThrottlesLargeDownloads()
        {
            ProxyManager.Instance.Activate("127.0.0.1", 9050);

            // Throttled features return true (allowed to proceed) but output a reason warning
            Assert.True(FeatureGate.Check("LargeDownload", out var reason));
            Assert.Contains("THROTTLED", reason);

            EnsureDeactivated();
        }

        [Fact]
        public void Check_WhenAnonymousModeActive_AllowsFileUploadAndSynFlood()
        {
            ProxyManager.Instance.Activate("127.0.0.1", 9050);

            Assert.True(FeatureGate.Check("FileUpload", out var reasonUpload));
            Assert.Empty(reasonUpload);

            Assert.True(FeatureGate.Check("SynFlood", out var reasonSyn));
            Assert.Empty(reasonSyn);

            EnsureDeactivated();
        }

        [Fact]
        public void Enforce_WhenBlocked_ThrowsAnonymousFeatureBlockedException()
        {
            ProxyManager.Instance.Activate("127.0.0.1", 9050);

            var ex = Assert.Throws<AnonymousFeatureBlockedException>(() => FeatureGate.Enforce("UDP"));
            Assert.Equal("UDP", ex.FeatureName);

            EnsureDeactivated();
        }
    }
}
