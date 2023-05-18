using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class RelaysViewModel : BaseViewModel, ITransientDependency
{
    public RelaysViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
        Title = L["Relays"];
    }
}