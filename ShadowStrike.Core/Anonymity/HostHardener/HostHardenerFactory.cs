using System;
using System.Runtime.InteropServices;

namespace ShadowStrike.Core.Anonymity.HostHardener
{
    public static class HostHardenerFactory
    {
        public static IHostHardener Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsHostHardener();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new MacOsHostHardener();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxHostHardener();
            }

            throw new PlatformNotSupportedException("Unsupported operating system.");
        }
    }
}
