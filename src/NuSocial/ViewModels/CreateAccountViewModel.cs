using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using CommunityToolkit.Maui.Alerts;
using Secp256k1Net;

namespace NuSocial.ViewModels
{
    public partial class CreateAccountViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _about = string.Empty;

        [ObservableProperty]
        private string _key = string.Empty;

        public override void OnAppearing()
        {
            base.OnAppearing();

            RegenerateKey();
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private void RegenerateKey()
        {
            using var secp256k1 = new Secp256k1();

            // Generate a private key.
            var privateKey = new byte[32];
            using var rnd = System.Security.Cryptography.RandomNumberGenerator.Create();
            do { rnd.GetBytes(privateKey); }
            while (!secp256k1.SecretKeyVerify(privateKey));

            // Create public key from private key.
            var publicKey = new byte[64];
            if (secp256k1.PublicKeyCreate(publicKey, privateKey))
            {
                // TODO: wtf, totally not right.
                var compressedPublicKey = new byte[64];
                if (secp256k1.PublicKeySerialize(compressedPublicKey, publicKey, Flags.SECP256K1_EC_COMPRESSED))
                {
                    var t = BitConverter.ToString(compressedPublicKey).Replace("-", string.Empty).ToLowerInvariant();
                    Key = t.TrimEnd('0');
                }
            }
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task CopyKey()
        {
            return SetBusyAsync(async () =>
            {
                await Clipboard.Default.SetTextAsync(Key).ConfigureAwait(false);
                await Snackbar.Make("Copied.", duration: TimeSpan.FromSeconds(5)).Show().ConfigureAwait(false);
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
        private Task CreateAccountAsync()
        {
            return SetBusyAsync(() =>
            {
                return Task.CompletedTask;
            });
        }
    }
}
