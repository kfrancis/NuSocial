using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Core.ViewModel;
using NuSocial.Messages;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class MainViewModel : BaseViewModel, ITransientDependency
{
    [ObservableProperty]
    private User? _user;

    public MainViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }

    public override Task OnFirstAppear()
    {
        WeakReferenceMessenger.Default.Send<ResetNavMessage>(new());
        return Task.CompletedTask;
    }

    public override Task OnParameterSet()
    {
        return SetBusyAsync(() =>
        {
            if (NavigationParameter is User user)
            {
                User = user;
            }

            return Task.CompletedTask;
        });
    }
}