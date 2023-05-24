using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class StartViewModel : BaseViewModel, ITransientDependency
{
    public StartViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
        
    }

    public override Task InitializeAsync()
    {
        Title = L["NuSocial"];
        return base.InitializeAsync();
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task CreateAccountAsync()
    {
        return SetBusyAsync(() =>
        {
            return Navigation.NavigateTo(nameof(AgreeViewModel));
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task GoToLoginAsync()
    {
        return SetBusyAsync(() =>
        {
            return Navigation.NavigateTo(nameof(LoginViewModel));
        });
    }
}