using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class MessagesViewModel : BaseViewModel, ITransientDependency
{
    public string? MessageContentProperty { get; set; }

    public MessagesViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }
}