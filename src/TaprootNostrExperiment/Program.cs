using NBitcoin;
using Nostr.Client.Keys;

namespace TaprootNostrExperiment;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            var mnemo = new Mnemonic("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about");
            var root = mnemo.DeriveExtKey();
            var change = new Key();
            var rootKey = new ExtKey();
            var accountKeyPath = new KeyPath("86'/0'/0'");
            var accountRootKeyPath = new RootedKeyPath(rootKey.GetPublicKey().GetHDFingerPrint(), accountKeyPath);
            var accountKey = rootKey.Derive(accountKeyPath);
            var key = accountKey.Derive(new KeyPath("0/0")).PrivateKey;
            var address = key.PubKey.GetAddress(ScriptPubKeyType.TaprootBIP86, Network.Main);

            var addr = key.PubKey.GetTaprootFullPubKey().GetAddress(Network.Main);

            var keyPair = NostrKeyPair.GenerateNew();

            Console.WriteLine("Done");
        }
        catch (Exception)
        {

            throw;
        }
        
        
    }
}