using NostrLib.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NostrLib.Models
{
    public class NostrSubscriptionFilter
    {
        [JsonPropertyName("authors")] public string[]? Authors { get; set; }
        [JsonPropertyName("#e")] public string[]? EventId { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JsonElement> ExtensionData { get; set; }

        [JsonPropertyName("ids")] public string[]? Ids { get; set; }
        [JsonPropertyName("kinds")] public NostrKind[]? Kinds { get; set; }
        [JsonPropertyName("limit")] public int? Limit { get; set; }
        [JsonPropertyName("#p")] public string[]? PublicKey { get; set; }
        [JsonPropertyName("since")][JsonConverter(typeof(UnixTimestampSecondsJsonConverter))] public DateTimeOffset? Since { get; set; } = DateTimeOffset.MinValue;
        [JsonPropertyName("until")][JsonConverter(typeof(UnixTimestampSecondsJsonConverter))] public DateTimeOffset? Until { get; set; } = DateTimeOffset.MaxValue;

        public Dictionary<string, string[]> GetAdditionalTagFilters()
        {
            var tagFilters = ExtensionData?.Where(pair => pair.Key.StartsWith("#") && pair.Value.ValueKind == JsonValueKind.Array);
            return tagFilters?.ToDictionary(tagFilter => tagFilter.Key.Substring(1),
                tagFilter => tagFilter.Value.EnumerateArray().ToEnumerable().Select(element => element.GetString())
                    .ToArray())! ?? new Dictionary<string, string[]>();
        }
    }
}