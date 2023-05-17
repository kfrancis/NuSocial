using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class MainViewModel : BaseViewModel, ITransientDependency
{
    public MainViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }
}