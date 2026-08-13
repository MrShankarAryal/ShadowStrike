using System;
using System.Text.RegularExpressions;
using ShadowStrike.Core.Anonymity.HostHardener;
using Xunit;

namespace ShadowStrike.Tests
{
    /// <summary>
    /// Unit tests for <see cref="MacRandomizer"/>.
    /// Validates cryptographic correctness of MAC generation:
    ///   - Proper format (XX:XX:XX:XX:XX:XX)
    ///   - Unicast bit (bit 0 of first octet = 0)
    ///   - Locally-administered bit (bit 1 of first octet = 1)
    ///   - Sufficient entropy (no two consecutive calls return the same value)
    /// </summary>
    public class MacRandomizerTests
    {
        private static readonly Regex MacPattern =
            new(@"^[0-9A-F]{2}(:[0-9A-F]{2}){5}$", RegexOptions.Compiled);

        private static readonly Regex RegistryPattern =
            new(@"^[0-9A-F]{12}$", RegexOptions.Compiled);

        private static readonly Regex LinuxPattern =
            new(@"^[0-9a-f]{2}(:[0-9a-f]{2}){5}$", RegexOptions.Compiled);

        // ── Format tests ─────────────────────────────────────────────────────

        [Fact]
        public void Generate_ReturnsCorrectFormat()
        {
            var mac = MacRandomizer.Generate();
            Assert.Matches(MacPattern, mac);
        }

        [Fact]
        public void GenerateRegistryFormat_ReturnsCorrectFormat()
        {
            var mac = MacRandomizer.GenerateRegistryFormat();
            Assert.Matches(RegistryPattern, mac);
            Assert.Equal(12, mac.Length);
        }

        [Fact]
        public void GenerateLinuxFormat_ReturnsCorrectFormat()
        {
            var mac = MacRandomizer.GenerateLinuxFormat();
            Assert.Matches(LinuxPattern, mac);
        }

        // ── Bit-level correctness ─────────────────────────────────────────────

        [Theory]
        [InlineData(100)] // run 100 times to catch any statistical bias
        public void Generate_FirstOctet_IsUnicastAndLocallyAdministered(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                var mac = MacRandomizer.Generate();
                var firstOctet = Convert.ToByte(mac.Split(':')[0], 16);

                // bit 0 must be 0 → unicast
                Assert.Equal(0, firstOctet & 0x01);

                // bit 1 must be 1 → locally administered
                Assert.Equal(0x02, firstOctet & 0x02);
            }
        }

        [Theory]
        [InlineData(100)]
        public void GenerateRegistryFormat_FirstOctet_IsUnicastAndLocallyAdministered(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                var mac = MacRandomizer.GenerateRegistryFormat();
                var firstOctet = Convert.ToByte(mac.Substring(0, 2), 16);
                Assert.Equal(0, firstOctet & 0x01);
                Assert.Equal(0x02, firstOctet & 0x02);
            }
        }

        // ── Entropy test ─────────────────────────────────────────────────────

        [Fact]
        public void Generate_ProducesUniqueValues()
        {
            // Generate 1000 MACs and assert all are unique
            // Probability of collision with 48-bit address and proper CSPRNG is negligible
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                var mac = MacRandomizer.Generate();
                Assert.True(seen.Add(mac), $"MAC collision detected: {mac} was generated twice within 1000 iterations.");
            }
        }

        // ── IsLocallyAdministered validator ──────────────────────────────────

        [Theory]
        [InlineData("02:4A:F1:9C:3E:B7", true)]   // LA bit set, unicast
        [InlineData("06:4A:F1:9C:3E:B7", true)]   // LA bit set, unicast (bits 1+2)
        [InlineData("00:4A:F1:9C:3E:B7", false)]  // not locally administered
        [InlineData("01:4A:F1:9C:3E:B7", false)]  // multicast (bit 0 set)
        [InlineData("03:4A:F1:9C:3E:B7", false)]  // multicast + LA (bit 0 set)
        public void IsLocallyAdministered_ReturnsExpected(string mac, bool expected)
        {
            Assert.Equal(expected, MacRandomizer.IsLocallyAdministered(mac));
        }

        [Fact]
        public void Generate_IsLocallyAdministered_AlwaysTrue()
        {
            for (int i = 0; i < 500; i++)
            {
                var mac = MacRandomizer.Generate();
                Assert.True(MacRandomizer.IsLocallyAdministered(mac),
                    $"Generated MAC '{mac}' failed the IsLocallyAdministered check.");
            }
        }
    }
}
