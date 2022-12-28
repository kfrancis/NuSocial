using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NostrLib.Converters;

namespace NostrLib.Models
{
    public class NostrSubscriptionFilter
    {
        [JsonPropertyName("authors")] public Collection<string> Authors { get; } = new();
        [JsonPropertyName("#e")] public Collection<string>? EventId { get; }

        [JsonExtensionData]
        public IDictionary<string, JsonElement> ExtensionData { get; }

        [JsonPropertyName("ids")] public Collection<string>? Ids { get; }
        [JsonPropertyName("kinds")] public Collection<NostrKind> Kinds { get; } = new();
        [JsonPropertyName("limit")] public int? Limit { get; set; }
        [JsonPropertyName("#p")] public Collection<string>? PublicKey { get; }
        [JsonPropertyName("since")][JsonConverter(typeof(UnixTimestampSecondsJsonConverter))] public DateTimeOffset? Since { get; set; } = DateTimeOffset.MinValue;
        [JsonPropertyName("until")][JsonConverter(typeof(UnixTimestampSecondsJsonConverter))] public DateTimeOffset? Until { get; set; } = DateTimeOffset.MaxValue;

        public Dictionary<string, string[]> GetAdditionalTagFilters()
        {
            var tagFilters = ExtensionData?.Where(pair => pair.Key.StartsWith("#", StringComparison.InvariantCultureIgnoreCase) && pair.Value.ValueKind == JsonValueKind.Array);
            return tagFilters?.ToDictionary(tagFilter => tagFilter.Key[1..],
                tagFilter => tagFilter.Value.EnumerateArray().ToEnumerable().Select(element => element.GetString() ?? string.Empty)
                    .ToArray())! ?? new Dictionary<string, string[]>();
        }
    }
}