using Nostr.Client.Keys;
using SQLite;

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