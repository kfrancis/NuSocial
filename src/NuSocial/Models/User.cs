using NBitcoin.Secp256k1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Models
{
    public record User
    {
        public string? Username { get; set; }
        public string Key { get; set; }
        public ECPrivKey? GetKey() => ECPrivKey.TryCreateFromDer(Convert.FromHexString(Key), out var res) ? res : null;
    }

    public record Relay
    {
        public Uri? Uri { get; set; }
    }
}
