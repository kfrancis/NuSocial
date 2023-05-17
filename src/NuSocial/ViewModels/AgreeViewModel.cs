using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class AgreeViewModel : BaseViewModel, ITransientDependency
{
    public AgreeViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task AgreeAsync()
    {
        return SetBusyAsync(() =>
        {
            // Record agreement, move to login
            return Navigation.NavigateTo(nameof(RegisterViewModel));
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task DisagreeAsync()
    {
        return SetBusyAsync(() =>
        {
            // Alert prompt, make sure the user is sure - then nav back to the start
            return Task.CompletedTask;
        });
    }
}