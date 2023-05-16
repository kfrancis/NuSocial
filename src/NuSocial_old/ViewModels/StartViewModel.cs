using NBitcoin;
using NuSocial.Core.Threading;

namespace NuSocial.ViewModels
{
    public partial class StartViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        public StartViewModel(IDialogService dialogService,
                              ICustomDispatcher customDispatcher,
                              ISettingsService settingsService)
            : base(dialogService, customDispatcher)
        {
            _settingsService = settingsService;
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task GoToCreateAccountAsync()
        {
            return SetBusyAsync(() =>
            {
                return Shell.Current.GoToAsync("//createAccount", true);
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task GoToLoginAsync()
        {
            return SetBusyAsync(() =>
            {
                return Shell.Current.GoToAsync("//login", true);
            });
        }
    }
}
