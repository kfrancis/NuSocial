using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using NostrLib.Converters;

namespace NostrLib.Models
{
    [JsonConverter(typeof(NostrEventTagJsonConverter))]
    public class NostrEventTag
    {
        public List<string> Data { get; set; } = new();

        [JsonIgnore] public INostrEvent Event { get; set; }
        public string EventId { get; set; }

        public string Id { get; set; }
        public string TagIdentifier { get; set; }

        public override string ToString()
        {
            var d = TagIdentifier is null ? Data : Data.Prepend(TagIdentifier);
            return JsonSerializer.Serialize(d, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }
}