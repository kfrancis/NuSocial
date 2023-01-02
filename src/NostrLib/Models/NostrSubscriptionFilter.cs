using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NostrLib.Converters;
using System.Reflection;

namespace NostrLib.Models
{
    public class NostrSubscriptionFilter
    {
        [JsonIgnore]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public Collection<string> Authors { get; set; }

        [JsonPropertyName("authors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public Collection<string>? AuthorsExtensions
        {
            get => Authors?.Count > 0 ? Authors : null;
            set => Authors = value ?? new();
        }

        [JsonIgnore]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public Collection<string>? EventId { get; set; }

        [JsonPropertyName("#e")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public Collection<string>? EventIdExtensions
        {
            get => EventId?.Count > 0 ? EventId : null;
            set => EventId = value ?? new();
        }

        [JsonExtensionData]
        public IDictionary<string, JsonElement> ExtensionData { get; }

        [JsonIgnore]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public Collection<string>? Ids { get; set; }

        [JsonPropertyName("extension")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public Collection<string>? IdsExtensions
        {
            get => Ids?.Count > 0 ? Ids : null;
            set => Ids = value ?? new();
        }

        [JsonPropertyName("kinds")]
        public Collection<int> Kinds { get; } = new();

        [JsonPropertyName("limit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Limit { get; set; }

        [JsonIgnore]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public Collection<string>? PublicKey { get; set; }

        [JsonPropertyName("#p")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public Collection<string>? PublicKeyExtensions
        {
            get => PublicKey?.Count > 0 ? PublicKey : null;
            set => PublicKey = value ?? new();
        }

        [JsonPropertyName("since")]
        [JsonConverter(typeof(UnixTimestampSecondsJsonConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? Since { get; set; }

        [JsonPropertyName("until")]
        [JsonConverter(typeof(UnixTimestampSecondsJsonConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? Until { get; set; }

        public Dictionary<string, string[]> GetAdditionalTagFilters()
        {
            var tagFilters = ExtensionData?.Where(pair => pair.Key.StartsWith("#", StringComparison.InvariantCultureIgnoreCase) && pair.Value.ValueKind == JsonValueKind.Array);
            return tagFilters?.ToDictionary(tagFilter => tagFilter.Key[1..],
                tagFilter => tagFilter.Value.EnumerateArray().ToEnumerable().Select(element => element.GetString() ?? string.Empty)
                    .ToArray())! ?? new Dictionary<string, string[]>();
        }
    }
}
