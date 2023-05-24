using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class AgreeViewModel : BaseViewModel, ITransientDependency
{
    public AgreeViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
        Title = L["EULA"];
    }

    [ObservableProperty]
    private string _eula = string.Empty;

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task AgreeAsync()
    {
        return SetBusyAsync(() =>
        {
            // Record agreement, move to login
            return Navigation.NavigateTo(nameof(RegisterViewModel));
        });
    }

    [ObservableProperty]
    private bool _agreeEnabled = true;

    public Task EulaScrolledAsync(object sender, ScrolledEventArgs args)
    {
        return SetBusyAsync(() =>
        {
            if (sender is ScrollView scrollView)
            {
                // If the user has scrolled to the bottom, enable the agree button
                if (scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height)
                {
                    AgreeEnabled = true;
                }
                else
                {
                    AgreeEnabled = false;
                }
            }
            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task DisagreeAsync()
    {
        return SetBusyAsync(() =>
        {
            // Alert prompt, make sure the user is sure - then nav back to the start
            return Navigation.NavigateTo("//start");
        });
    }

    public override Task OnFirstAppear()
    {
        Eula = L["AgreementText"];
        return Task.CompletedTask;
    }
}
