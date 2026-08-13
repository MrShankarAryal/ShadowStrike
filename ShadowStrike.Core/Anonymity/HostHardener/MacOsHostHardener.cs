using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ShadowStrike.Core.Anonymity.HostHardener
{
    public class MacOsHostHardener : IHostHardener
    {
        public Task SpoofMacAddressAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new PlatformNotSupportedException("MacOsHostHardener is only supported on macOS.");
            return Task.CompletedTask;
        }

        public Task RandomizeHostnameAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new PlatformNotSupportedException("MacOsHostHardener is only supported on macOS.");
            return Task.CompletedTask;
        }

        public Task FlushDnsAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new PlatformNotSupportedException("MacOsHostHardener is only supported on macOS.");
            return Task.CompletedTask;
        }

        public Task DisableTelemetryAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new PlatformNotSupportedException("MacOsHostHardener is only supported on macOS.");
            return Task.CompletedTask;
        }

        public Task<HardeningReport> VerifyAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new PlatformNotSupportedException("MacOsHostHardener is only supported on macOS.");
            return Task.FromResult(new HardeningReport());
        }
    }
}
