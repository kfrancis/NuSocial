using System.Security.Cryptography;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;
using NBitcoin.Secp256k1;
using NostrLib;
using NostrLib.Models;
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
        private string _publicKey = string.Empty;

        [ObservableProperty]
        private string _privateKey = string.Empty;

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
                await Clipboard.Default.SetTextAsync(PublicKey).ConfigureAwait(false);
                await Snackbar.Make(AppResources.Copied, duration: TimeSpan.FromSeconds(5)).Show().ConfigureAwait(false);
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task CreateAccountAsync()
        {
            return SetBusyAsync(async () =>
            {
                var popup = new KeyView(new NostrKeyPair(PublicKey, PrivateKey));
                await AppShell.Current.ShowPopupAsync(popup);
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
            var tempKey = NostrClient.GenerateKey();

            PublicKey = tempKey.PublicKey;
            PrivateKey = tempKey.PrivateKey;
        }
    }
}
