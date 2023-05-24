using Microsoft.Extensions.Configuration;
using Nostr.Client.Keys;
using NuSocial.Core.Validation;
using NuSocial.Core.ViewModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class LoginViewModel : BaseFormModel, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IDatabase _db;

    [Required]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAccountKeyValid))]
    [NostrKeyValid]
    private string _accountKey = string.Empty;

    public LoginViewModel(IDialogService dialogService,
                          INavigationService navigationService,
                          IConfiguration configuration,
                          IDatabase db) : base(dialogService, navigationService)
    {
        Title = L["Login"];
        _configuration = configuration;
        _db = db;
    }

    public bool IsAccountKeyValid => !string.IsNullOrEmpty(AccountKey);
    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task DemoAsync()
    {
        return SetBusyAsync(async () =>
        {
            var keyPair = NostrKeyPair.GenerateNew();

            var user = new User() { PublicKey = keyPair.PublicKey, PrivateKey = keyPair.PrivateKey };
            await _db.UpdateUsersAsync(new ObservableCollection<User> { user });
            GlobalSetting.Instance.DemoMode = true;
            GlobalSetting.Instance.CurrentUser = user;
            await Navigation.NavigateTo("//main", user);
        });
    }

    [ObservableProperty]
    private bool _showDemoButton;

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
                        GlobalSetting.Instance.CurrentUser = user;
                        await Navigation.NavigateTo("//main", user);
                    }
                }
            }, async () => await ShowErrorsCommand.ExecuteAsync(null));
        });
    }

    public override Task OnFirstAppear()
    {
        SetWhenDebug();
        return Task.CompletedTask;
    }

    [Conditional("DEBUG")]
    private void SetWhenDebug()
    {
        if (_configuration["Keys:Public"] is string pubKey && !string.IsNullOrEmpty(pubKey))
        {
            AccountKey = pubKey;
        }

        ShowDemoButton = true;
    }
}
