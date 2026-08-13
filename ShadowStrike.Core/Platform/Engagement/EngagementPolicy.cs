using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShadowStrike.Core.Platform.Engagement
{
    public class TargetScope
    {
        [JsonPropertyName("host")]
        public string Host { get; set; } = string.Empty;

        [JsonPropertyName("protocols")]
        public List<string> Protocols { get; set; } = new();

        [JsonPropertyName("maxRps")]
        public int MaxRps { get; set; } = 50;

        [JsonPropertyName("destructive")]
        public bool Destructive { get; set; } = false;
    }

    public class EngagementPolicy
    {
        [JsonPropertyName("engagement")]
        public string Engagement { get; set; } = string.Empty;

        [JsonPropertyName("client")]
        public string Client { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; set; } = string.Empty;

        [JsonPropertyName("validFrom")]
        public DateTime ValidFrom { get; set; }

        [JsonPropertyName("validUntil")]
        public DateTime ValidUntil { get; set; }

        [JsonPropertyName("targets")]
        public List<TargetScope> Targets { get; set; } = new();

        [JsonPropertyName("allowedModes")]
        public List<string> AllowedModes { get; set; } = new();

        [JsonPropertyName("destructiveTests")]
        public bool DestructiveTests { get; set; } = false;

        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;
    }
}
