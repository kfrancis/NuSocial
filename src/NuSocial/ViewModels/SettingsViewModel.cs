using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class SettingsViewModel : BaseViewModel, ITransientDependency
{
    public SettingsViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }
}