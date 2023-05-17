using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class ProfileViewModel : BaseViewModel, ITransientDependency
{
    public ProfileViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }
}