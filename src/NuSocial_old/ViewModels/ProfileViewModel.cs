using NuSocial.Core.Threading;
using NuSocial.Resources.Strings;

namespace NuSocial.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    public ProfileViewModel(IDialogService dialogService, ICustomDispatcher customDispatcher) : base(dialogService, customDispatcher)
    {
        Title = AppResources.Profile;
    }
}
