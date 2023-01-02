using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
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
