using System;
using System.Text.RegularExpressions;

namespace ShadowStrike.Core.Anonymity.VmOrchestrator
{
    public class TopologyViolationException : Exception
    {
        public TopologyViolationException(string message) : base(message) { }
    }

    public static class VmTopologyChecker
    {
        /// <summary>
        /// Validates output from VBoxManage showvminfo for Whonix-Gateway and Whonix-Workstation.
        /// Non-negotiable rules:
        /// Gateway: nic1=NAT, nic2=intnet ("Whonix")
        /// Workstation: nic1=intnet ("Whonix"), NO nic2/NAT/Bridged adapter.
        /// </summary>
        public static void VerifyTopology(string gatewayInfo, string workstationInfo)
        {
            // Verify Gateway
            if (!Regex.IsMatch(gatewayInfo, @"NIC 1:.*Attachment: NAT", RegexOptions.IgnoreCase))
            {
                throw new TopologyViolationException("Whonix-Gateway NIC 1 must be set to NAT.");
            }
            if (!Regex.IsMatch(gatewayInfo, @"NIC 2:.*Attachment: Internal Network", RegexOptions.IgnoreCase))
            {
                throw new TopologyViolationException("Whonix-Gateway NIC 2 must be set to Internal Network.");
            }

            // Verify Workstation
            if (!Regex.IsMatch(workstationInfo, @"NIC 1:.*Attachment: Internal Network", RegexOptions.IgnoreCase))
            {
                throw new TopologyViolationException("ERR_NET_TOPOLOGY_VIOLATION: Whonix-Workstation NIC 1 must be set to Internal Network.");
            }

            // Workstation must NOT have NIC 2 or NAT/Bridged on any interface
            if (Regex.IsMatch(workstationInfo, @"NIC 2:.*(NAT|Bridged|Host-only)", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(workstationInfo, @"NIC 1:.*(NAT|Bridged)", RegexOptions.IgnoreCase))
            {
                throw new TopologyViolationException("ERR_NET_TOPOLOGY_VIOLATION: Whonix-Workstation contains unauthorized NAT or Bridged adapter!");
            }
        }
    }
}
