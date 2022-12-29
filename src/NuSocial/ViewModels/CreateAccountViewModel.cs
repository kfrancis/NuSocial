using System.Security.Cryptography;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui;
using NuSocial.Resources.Strings;
using Secp256k1Net;
using static SQLite.SQLite3;

namespace NuSocial.ViewModels
{
    public partial class CreateAccountViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _about = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _key = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        public override void OnAppearing()
        {
            base.OnAppearing();

            RegenerateKey();
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task CopyKey()
        {
            return SetBusyAsync(async () =>
            {
                await Clipboard.Default.SetTextAsync(Key).ConfigureAwait(false);
                await Snackbar.Make(AppResources.Copied, duration: TimeSpan.FromSeconds(5)).Show().ConfigureAwait(false);
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task CreateAccountAsync()
        {
            return SetBusyAsync(() =>
            {
                return Task.CompletedTask;
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task GoToStartAsync()
        {
            return SetBusyAsync(() =>
            {
                return Shell.Current.GoToAsync("//start", true);
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private void RegenerateKey()
        {
            //// Generate a new secp256k1 key pair
            //using var ecdsa = new ECDsaCng(ECCurve.CreateFromValue("1.3.132.0.10"));

            //// Get the public key
            //var publicKey = ecdsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);

            //// Get the private key
            //var privateKey = ecdsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
            //var t = BitConverter.ToString(publicKey).Replace("-", string.Empty).ToLowerInvariant();
            //Key = t;

            using var secp256k1 = new Secp256k1();

            // Generate a private key.
            var privateKey = new byte[32];
            using var rnd = RandomNumberGenerator.Create();
            do { rnd.GetBytes(privateKey); }
            while (!secp256k1.SecretKeyVerify(privateKey));

            // Create public key from private key.
            var publicKey = new byte[64];
            if (secp256k1.PublicKeyCreate(publicKey, privateKey))
            {
                // Serialize the public key
                Span<byte> serializedKey = new byte[Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH];
                if (secp256k1.PublicKeySerialize(serializedKey, publicKey, Flags.SECP256K1_EC_COMPRESSED))
                {
                    var pubKeyHex = BitConverter.ToString(serializedKey.ToArray()).Replace("-", string.Empty).ToLowerInvariant();
                    Key = pubKeyHex;
                }

                //// TODO: wtf, totally not right.
                //var compressedPublicKey = new byte[64];
                //if (secp256k1.PublicKeySerialize(compressedPublicKey, publicKey, Flags.SECP256K1_EC_COMPRESSED))
                //{
                //    var t = BitConverter.ToString(compressedPublicKey).Replace("-", string.Empty).ToLowerInvariant();
                //     = t.TrimEnd('0');
                //}
            }
        }
    }
}
