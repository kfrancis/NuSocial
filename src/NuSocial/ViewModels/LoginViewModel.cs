using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class LoginViewModel : BaseFormModel, ITransientDependency
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAccountKeyValid))]
    private string _accountKey = string.Empty;

    public LoginViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
        Title = L["Login"];
    }

    public bool IsAccountKeyValid => !string.IsNullOrEmpty(AccountKey);

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task LoginAsync()
    {
        return SetBusyAsync(() => { return Task.CompletedTask; });
    }
}