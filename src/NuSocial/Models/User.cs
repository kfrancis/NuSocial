using Nostr.Client.Keys;
using SQLite;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace NuSocial.Models;

public class User
{
    [Ignore]
    public NostrPublicKey? PublicKey { get; set; }

    [Ignore]
    public NostrPrivateKey? PrivateKey { get; set; }

    public string? ProfileImageBlurHash { get; set; }

    [PrimaryKey]
    public string PublicKeyString
    {
        get => PublicKey?.Bech32 ?? string.Empty;
        set => PublicKey = NostrPublicKey.FromBech32(value);
    }

    public string PrivateKeyString
    {
        get => PrivateKey?.Bech32 ?? string.Empty;
        set => PrivateKey = NostrPrivateKey.FromBech32(value);
    }

    public bool IsReadOnly => PublicKey != null && PrivateKey == null;
}

public class AuthenticateResult
{
    public NostrKeyPair KeyPair { get; set; }
}

public class AuthenticateRequest
{

}

public class UserConfiguration
{

}

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

    [JsonIgnore]
    public string PetName { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonIgnore]
    public string? Nip05 { get; set; }

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