using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class WalletViewModel : BaseViewModel, ITransientDependency
{
    public WalletViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }
}