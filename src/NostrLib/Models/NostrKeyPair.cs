namespace NostrLib.Models
{
    public class NostrKeyPair
    {
        public NostrKeyPair(string pubKey, string privKey)
        {
            PublicKey = pubKey;
            PrivateKey = privKey;
        }

        public string PrivateKey { get; private set; }
        public string PublicKey { get; private set; }
    }
}