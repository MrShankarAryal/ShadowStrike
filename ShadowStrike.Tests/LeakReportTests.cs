using ShadowStrike.Core.Anonymity.LeakGuardSystem;
using Xunit;

namespace ShadowStrike.Tests
{
    public class LeakReportTests
    {
        [Fact]
        public void Verdict_WhenAllChecksPass_ReturnsPASS()
        {
            var report = new LeakReport
            {
                IsTorExitNode = true,
                DnsLeaked = false,
                WebRtcLeaked = false,
                MacSpoofed = true,
                HostnameRandomized = true,
                VmTopologyValid = true
            };

            Assert.Equal("PASS", report.Verdict);
        }

        [Fact]
        public void Verdict_WhenNotTorExitNode_ReturnsFAIL()
        {
            var report = new LeakReport
            {
                IsTorExitNode = false,
                DnsLeaked = false,
                WebRtcLeaked = false,
                VmTopologyValid = true
            };

            Assert.Equal("FAIL", report.Verdict);
        }

        [Fact]
        public void Verdict_WhenDnsLeaked_ReturnsFAIL()
        {
            var report = new LeakReport
            {
                IsTorExitNode = true,
                DnsLeaked = true,
                WebRtcLeaked = false,
                VmTopologyValid = true
            };

            Assert.Equal("FAIL", report.Verdict);
        }

        [Fact]
        public void Verdict_WhenVmTopologyInvalid_ReturnsFAIL()
        {
            var report = new LeakReport
            {
                IsTorExitNode = true,
                DnsLeaked = false,
                WebRtcLeaked = false,
                VmTopologyValid = false
            };

            Assert.Equal("FAIL", report.Verdict);
        }
    }
}
