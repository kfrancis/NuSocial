using NostrLib;
using NuSocial.Core.Threading;

namespace NuSocial.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _key = string.Empty;
        private readonly ISettingsService _settingsService;
        private readonly IDatabase _db;

        [ObservableProperty]
        private ObservableCollection<RelayItem> _relayItems = new();

        public LoginViewModel(IDialogService dialogService, ICustomDispatcher customDispatcher, ISettingsService settingsService, IDatabase db) : base(dialogService, customDispatcher)
        {
            _settingsService = settingsService;
            _db = db;
        }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            // If we already have a key stored, use it.
            Key = await SecureStorage.Default.GetAsync("key");


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
