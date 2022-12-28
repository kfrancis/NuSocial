using NBitcoin.Secp256k1;
using SQLite;
using System.Text.Json.Serialization;

namespace NuSocial.Models
{
    public record Post
    {
        public Contact Contact { get; set; }

        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record User
    {
        [PrimaryKey]
        public string Key { get; set; }

        public string? Username { get; set; }

        public string? BlurHash { get; set; }

        public ECPrivKey? GetKey() => ECPrivKey.TryCreateFromDer(Convert.FromHexString(Key), out var res) ? res : null;
    }

    public record Relay
    {
        public Uri? Uri { get; set; }
    }

    public partial class Profile : ObservableObject
    {
    }

    /// <summary>
    /// A class for a query for contacts.
    /// </summary>
    /// <param name="Contacts">Gets the list of returned contacts.</param>
    public sealed record ContactsQueryResponse([property: JsonPropertyName("results")] IList<Contact> Contacts);

    /// <summary>
    /// A simple model for a contact.
    /// </summary>
    /// <param name="Name">Gets the name of the contact.</param>
    /// <param name="Email">Gets the email of the contact.</param>
    /// <param name="Picture">Gets the picture of the contact.</param>
    public sealed record Contact
    {
        [JsonPropertyName("name")]
        public Name Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonIgnore]
        public string PublicKey { get; set; }

        [JsonPropertyName("picture")]
        public Picture Picture { get; set; }

        public override string ToString()
        {
            return Name?.ToString() ?? PublicKey;
        }
    }

    /// <summary>
    /// A simple model for the name of a contact.
    /// </summary>
    /// <param name="First">The first name of the contact.</param>
    /// <param name="Last">The last name of the contact.</param>
    public sealed record Name(
        [property: JsonPropertyName("first")] string First,
        [property: JsonPropertyName("last")] string Last)
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{First} {Last}";
        }
    }

    /// <summary>
    /// A simple model for the picture of a contact.
    /// </summary>
    /// <param name="Url">The URL of the picture.</param>
    public sealed record Picture([property: JsonPropertyName("thumbnail")] Uri Url);
}
