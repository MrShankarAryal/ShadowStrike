using ShadowStrike.Core.Anonymity.VmOrchestrator;
using Xunit;

namespace ShadowStrike.Tests
{
    public class VmTopologyCheckerTests
    {
        [Fact]
        public void VerifyTopology_ValidTopology_Passes()
        {
            string gwInfo = @"
NIC 1:           MAC: 080027123456, Attachment: NAT, Cable connected: on
NIC 2:           MAC: 080027654321, Attachment: Internal Network 'Whonix', Cable connected: on";

            string wsInfo = @"
NIC 1:           MAC: 080027112233, Attachment: Internal Network 'Whonix', Cable connected: on
NIC 2:           disabled";

            // Should not throw exception
            VmTopologyChecker.VerifyTopology(gwInfo, wsInfo);
        }

        [Fact]
        public void VerifyTopology_GatewayNic1NotNat_Throws()
        {
            string gwInfo = @"
NIC 1:           Attachment: Bridged
NIC 2:           Attachment: Internal Network 'Whonix'";

            string wsInfo = @"NIC 1: Attachment: Internal Network 'Whonix'";

            Assert.Throws<TopologyViolationException>(() => VmTopologyChecker.VerifyTopology(gwInfo, wsInfo));
        }

        [Fact]
        public void VerifyTopology_WorkstationHasNat_ThrowsTopologyViolationException()
        {
            string gwInfo = @"
NIC 1:           Attachment: NAT
NIC 2:           Attachment: Internal Network 'Whonix'";

            string wsInfo = @"
NIC 1:           Attachment: Internal Network 'Whonix'
NIC 2:           Attachment: NAT";

            var ex = Assert.Throws<TopologyViolationException>(() => VmTopologyChecker.VerifyTopology(gwInfo, wsInfo));
            Assert.Contains("ERR_NET_TOPOLOGY_VIOLATION", ex.Message);
        }

        [Fact]
        public void VerifyTopology_WorkstationHasBridgedAdapter_ThrowsTopologyViolationException()
        {
            string gwInfo = @"
NIC 1:           Attachment: NAT
NIC 2:           Attachment: Internal Network 'Whonix'";

            string wsInfo = @"
NIC 1:           Attachment: Bridged";

            var ex = Assert.Throws<TopologyViolationException>(() => VmTopologyChecker.VerifyTopology(gwInfo, wsInfo));
            Assert.Contains("ERR_NET_TOPOLOGY_VIOLATION", ex.Message);
        }
    }
}
