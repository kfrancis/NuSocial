﻿using NBitcoin;
using Nostr.Client.Keys;
using SQLite;
using System.Text.Json.Serialization;

namespace NuSocial.Models;

public class User
{
    [Ignore]
    public NostrPublicKey? PublicKey { get; set; }

    [Ignore]
    public NostrPrivateKey? PrivateKey { get; set; }

    //private TaprootKeyPair? _taprootKey;

    //[Ignore]
    //public TaprootKeyPair TaprootKeyPair
    //{
    //    get
    //    {
    //        if (_taprootKey == null)
    //        {
    //            var key = Key.Parse(TaprootBech32m, Network.Main);
    //            var merkleRoot = RandomUtils.GetUInt256();
    //            _taprootKey = TaprootKeyPair.CreateTaprootPair(key, merkleRoot);
    //        }

    //        return _taprootKey;
    //    }
    //    set
    //    {
    //        _taprootKey = value;
    //    }
    //}

    //private string _taprootBech32m = string.Empty;
    //public string TaprootBech32m
    //{
    //    get
    //    {
    //        if (TaprootKeyPair != null)
    //        {
    //            return TaprootKeyPair.PubKey.GetAddress(Network.Main).PubKey.ToString();
    //        }
    //        else
    //        {
    //            return _taprootBech32m;
    //        }
    //    }
    //    set
    //    {
    //        _taprootBech32m = value;
    //    }
    //}

    public string Taproot { get; set; } = string.Empty;

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

public class Profile
{
    private string? _picture = "https://placehold.co/60x60.png";

    public string Name { get; internal set; } = string.Empty;
    public string? DisplayName { get; internal set; }
    public string? Picture
    {
        get
        {
            if (string.IsNullOrEmpty(_picture) || (_picture.Equals("https://placehold.co/60x60.png", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(Name)))
            {
                _picture = $"https://placehold.co/60x60.png?text={Name}";
            }
            return _picture;
        }
        internal set => _picture = value;
    }
    public string? Nip05 { get; internal set; }
    public string? About { get; internal set; }
    public string? Website { get; internal set; }
    public List<Relay> Relays { get; internal set; } = new();
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
/// A contact on the #nostr network
/// </summary>
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