using CommunityToolkit.Mvvm.Messaging;
using Nostr.Client.Keys;
using NuSocial.Core.Validation;
using NuSocial.Core.ViewModel;
using NuSocial.Messages;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class LoginViewModel : BaseFormModel, ITransientDependency
{
    private readonly IDatabase _db;

    [Required]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAccountKeyValid))]
    [NostrKeyValid]
    private string _accountKey = string.Empty;

    public LoginViewModel(IDialogService dialogService, INavigationService navigationService, IDatabase db) : base(dialogService, navigationService)
    {
        Title = L["Login"];
        _db = db;
    }

    public bool IsAccountKeyValid => !string.IsNullOrEmpty(AccountKey);

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task LoginAsync()
    {
        return SetBusyAsync(() =>
        {
            return ValidateAsync(async (isValid) =>
            {
                if (isValid)
                {
                    if (AccountKey.StartsWith("npub", StringComparison.OrdinalIgnoreCase))
                    {
                        var pubKey = NostrPublicKey.FromBech32(AccountKey);

                        var user = new User() { PublicKey = pubKey };
                        await _db.UpdateUsersAsync(new ObservableCollection<User> { user });
                        await Navigation.NavigateTo("//main", user);
                    }
                    else
                    {
                        var privKey = NostrPrivateKey.FromBech32(AccountKey);
                        var pubKeyEc = privKey.Ec.CreateXOnlyPubKey();
                        var pubKey = NostrPublicKey.FromEc(pubKeyEc);

                        var user = new User() { PublicKey = pubKey, PrivateKey = privKey };
                        await _db.UpdateUsersAsync(new ObservableCollection<User> { user });
                        await Navigation.NavigateTo("//main", user);
                    }
                }
            }, async () => await ShowErrorsCommand.ExecuteAsync(null));
        });
    }
}