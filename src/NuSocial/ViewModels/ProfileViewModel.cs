using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Core.ViewModel;
using NuSocial.Messages;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class ProfileViewModel : BaseViewModel, ITransientDependency
{
    public ProfileViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }

    public override Task InitializeAsync()
    {
        // We need to cancel any work currently handling nostr information, so fire that message.
        WeakReferenceMessenger.Default.Send<NostrStateChangeMessage>(new(false));

        return Task.CompletedTask;
    }
}