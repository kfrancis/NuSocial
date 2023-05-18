using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

[QueryProperty(nameof(MessageContentProperty), "set")]
public partial class MessagesViewModel : BaseViewModel, ITransientDependency
{
    public string? MessageContentProperty { get; set; }

    public MessagesViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }

    public override Task OnFirstAppear()
    {
        return base.OnFirstAppear();
    }

    public override Task OnParameterSet()
    {
        return SetBusyAsync(() =>
        {
            if (NavigationParameter is User user)
            {
                //User = user;
            }

            return Task.CompletedTask;
        });
    }
}