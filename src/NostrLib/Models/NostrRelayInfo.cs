using System.Text.Json.Serialization;

namespace NostrLib.Models
{
    public class NostrRelayInfo
    {
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; }

        [JsonPropertyName("pubkey")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Pubkey { get; set; }

        [JsonPropertyName("contact")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Contact { get; set; }

        [JsonPropertyName("supported_nips")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string SupportedNips { get; set; }

        [JsonPropertyName("software")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Software { get; set; }

        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Version { get; set; }
    }
}
