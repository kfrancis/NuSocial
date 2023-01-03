using System;
using System.Collections.Generic;
using System.Text;

namespace NostrLib.Models
{
    public class NostrKeyPair
    {
        public NostrKeyPair(string pubKey, string privKey)
        {
            PublicKey = pubKey;
            PrivateKey = privKey;
        }

        public string PublicKey { get; private set; }
        public string PrivateKey { get; private set; }
    }
}
