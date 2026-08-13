using System;
using System.Linq;
using System.Security.Cryptography;

namespace ShadowStrike.Core.Anonymity.HostHardener
{
    /// <summary>
    /// Generates cryptographically random, locally-administered unicast MAC addresses.
    ///
    /// Bit semantics of the first octet:
    ///   bit 0 (LSB) = multicast flag: MUST be 0 (unicast)
    ///   bit 1       = locally-administered flag: MUST be 1
    ///
    /// Operation: bytes[0] = (random & 0xFC) | 0x02
    ///   &amp; 0xFC clears bits 0 and 1  → ensures unicast (bit0=0) and clears the LA bit first
    ///   | 0x02  sets bit 1           → marks as locally-administered
    ///
    /// Result format: "XX:XX:XX:XX:XX:XX" (uppercase hex, colon-separated)
    /// </summary>
    public static class MacRandomizer
    {
        /// <summary>
        /// Generates a random, locally-administered unicast MAC address string.
        /// Example output: "02:4A:F1:9C:3E:B7"
        /// </summary>
        public static string Generate()
        {
            var bytes = new byte[6];
            RandomNumberGenerator.Fill(bytes);

            // Enforce unicast (bit 0 = 0) + locally administered (bit 1 = 1)
            // 0xFC = 1111 1100 → clears bits 0 and 1
            // 0x02 = 0000 0010 → sets bit 1
            bytes[0] = (byte)((bytes[0] & 0xFC) | 0x02);

            return string.Join(":", bytes.Select(b => b.ToString("X2")));
        }

        /// <summary>
        /// Returns the MAC address in Windows registry format (no separators).
        /// Example: "024AF19C3EB7"
        /// </summary>
        public static string GenerateRegistryFormat()
        {
            var bytes = new byte[6];
            RandomNumberGenerator.Fill(bytes);
            bytes[0] = (byte)((bytes[0] & 0xFC) | 0x02);
            return string.Concat(bytes.Select(b => b.ToString("X2")));
        }

        /// <summary>
        /// Returns the MAC address in Linux ip link format (lowercase colon-separated).
        /// Example: "02:4a:f1:9c:3e:b7"
        /// </summary>
        public static string GenerateLinuxFormat()
        {
            var bytes = new byte[6];
            RandomNumberGenerator.Fill(bytes);
            bytes[0] = (byte)((bytes[0] & 0xFC) | 0x02);
            return string.Join(":", bytes.Select(b => b.ToString("x2")));
        }

        /// <summary>
        /// Validates that a given MAC string is locally-administered and unicast.
        /// </summary>
        public static bool IsLocallyAdministered(string mac)
        {
            var parts = mac.Split(':', '-');
            if (parts.Length != 6) return false;
            if (!byte.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out var firstByte))
                return false;

            bool isUnicast = (firstByte & 0x01) == 0;  // bit 0 clear
            bool isLocal   = (firstByte & 0x02) != 0;  // bit 1 set
            return isUnicast && isLocal;
        }
    }
}
