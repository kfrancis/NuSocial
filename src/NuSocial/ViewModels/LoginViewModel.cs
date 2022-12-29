using NuSocial.Core.Threading;

namespace NuSocial.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _key = string.Empty;
        private readonly ISettingsService _settingsService;

        public LoginViewModel(IDialogService dialogService, ICustomDispatcher customDispatcher, ISettingsService settingsService) : base(dialogService, customDispatcher)
        {
            _settingsService = settingsService;
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task LoginAsync()
        {
            return SetBusyAsync(async () =>
            {
                if (await _settingsService.LoginAsync(Key))
                {
                    await Shell.Current.GoToAsync("//main", true);
                }
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task GoToStartAsync()
        {
            return SetBusyAsync(() =>
            {
                return Shell.Current.GoToAsync("//start", true);
            });
        }
    }
}
