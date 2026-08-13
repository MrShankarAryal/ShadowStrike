using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ShadowStrike.Core.Anonymity.HostHardener
{
    public class LinuxHostHardener : IHostHardener
    {
        public Task SpoofMacAddressAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new PlatformNotSupportedException("LinuxHostHardener is only supported on Linux.");
            return Task.CompletedTask;
        }

        public Task RandomizeHostnameAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new PlatformNotSupportedException("LinuxHostHardener is only supported on Linux.");
            return Task.CompletedTask;
        }

        public Task FlushDnsAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new PlatformNotSupportedException("LinuxHostHardener is only supported on Linux.");
            return Task.CompletedTask;
        }

        public Task DisableTelemetryAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new PlatformNotSupportedException("LinuxHostHardener is only supported on Linux.");
            return Task.CompletedTask;
        }

        public Task<HardeningReport> VerifyAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new PlatformNotSupportedException("LinuxHostHardener is only supported on Linux.");
            return Task.FromResult(new HardeningReport());
        }
    }
}
