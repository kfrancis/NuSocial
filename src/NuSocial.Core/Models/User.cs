using Nostr.Client.Keys;
using SQLite;
using SQLiteNetExtensions.Attributes;
using System.Text.Json.Serialization;

namespace NuSocial.Models;

[Table("Users")]
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

[Table("Profiles")]
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
    public List<Contact> Follows { get; internal set; } = new();
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
[Table("Contacts")]
public class Contact
{
    [JsonIgnore]
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

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

    public string? Relay { get; internal set; }

    public override string ToString()
    {
        return Name?.ToString() ?? PublicKey;
    }
}

[Table("MessageData")]
public class MessageData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime When { get; set; }
    public string Text { get; set; }
    public bool IsRead { get; set; }
    public bool IsIncoming { get; set; }

    [ForeignKey(typeof(Message))]
    public int MessageId { get; set; }
}

[Table("Messages")]
public class Message
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [ForeignKey(typeof(Contact))]
    public int ContactId { get; set; }

    [ManyToOne]
    public Contact? From { get; set; }

    [OneToMany(CascadeOperations = CascadeOperation.All)]
    public List<MessageData> Messages { get; set; } = new();

    public bool ContainsText(string filter)
    {
        return Messages.Any(x => x.Text.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }

    public DateTime LatestMessageDate
    {
        get
        {
            if (Messages.Count > 0)
            {
                return Messages.OrderByDescending(x => x.When).FirstOrDefault()?.When ?? DateTime.Now;
            }
            else
            {
                return DateTime.Now;
            }
        }
    }

    [Ignore]
    public string LatestMessage
    {
        get
        {
            if (Messages.Count > 0)
            {
                return Messages.OrderByDescending(x => x.When).FirstOrDefault()?.Text ?? string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}

public class MessageContactRelationshipTable
{
    public int ContactId { get; set; }
    public int MessageId { get; set; }
}

/// <summary>
/// A simple model for the name of a contact.
/// </summary>
public class Name
{
    [JsonPropertyName("first")]
    public string First { get; set; } = string.Empty;

    [JsonPropertyName("last")]
    public string Last { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{First} {Last}";
    }
}

/// <summary>
/// A simple model for the picture of a contact.
/// </summary>
public class Picture
{
    public Picture(Uri url)
    {
        Url = url;
    }

    public Picture()
    {

    }

    [JsonPropertyName("thumbnail")]
    public Uri Url { get; set; }

    public string Blurhash { get; set; }
}