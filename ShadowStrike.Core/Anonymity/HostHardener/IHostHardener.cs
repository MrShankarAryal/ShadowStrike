using System;
using System.Threading.Tasks;

namespace ShadowStrike.Core.Anonymity.HostHardener
{
    public class HardeningReport
    {
        public bool MacSpoofed { get; set; }
        public string SpoofedMac { get; set; } = string.Empty;
        public bool HostnameRandomized { get; set; }
        public string RandomizedHostname { get; set; } = string.Empty;
        public bool DnsFlushed { get; set; }
        public bool TelemetryDisabled { get; set; }
        public bool LlmnrDisabled { get; set; }
        public bool TeredoDisabled { get; set; }
        public string Warnings { get; set; } = string.Empty;
        public bool Success => MacSpoofed && HostnameRandomized && DnsFlushed && TelemetryDisabled;
    }

    public interface IHostHardener
    {
        Task SpoofMacAddressAsync();
        Task RandomizeHostnameAsync();
        Task FlushDnsAsync();
        Task DisableTelemetryAsync();
        Task<HardeningReport> VerifyAsync();
    }
}
