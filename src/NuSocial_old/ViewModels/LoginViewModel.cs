using NostrLib;
using NuSocial.Core.Threading;
using NuSocial.Resources.Strings;

namespace NuSocial.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IDatabase _db;

        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPrivateLabel))]
        private bool _isPrivate;

        [ObservableProperty]
        private string _key = string.Empty;

        [ObservableProperty]
        private ObservableCollection<RelayItem> _relayItems = new();

        public LoginViewModel(IDialogService dialogService, ICustomDispatcher customDispatcher, ISettingsService settingsService, IDatabase db) : base(dialogService, customDispatcher)
        {
            _settingsService = settingsService;
            _db = db;
        }

        public string IsPrivateLabel
        {
            get
            {
                return _isPrivate ? AppResources.Yes : AppResources.No;
            }
        }

        public override async void OnAppearing()
        {
            base.OnAppearing();

            // If we already have a key stored, use it.
            Key = await SecureStorage.Default.GetAsync("key");
            IsPrivate = await SecureStorage.Default.GetAsync("isPrivate") == "true";
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task GoToStartAsync()
        {
            return SetBusyAsync(() =>
            {
                return Shell.Current.GoToAsync("//start", true);
            });
        }

        [RelayCommand(CanExecute = "IsNotBusy")]
        private Task LoginAsync()
        {
            return SetBusyAsync(async () =>
            {
                if (await _settingsService.LoginAsync(Key, IsPrivate))
                {
                    await Shell.Current.GoToAsync("//main", true);
                }
                else
                {
                    // Couldn't login for some reason, tell the user.
                    await Shell.Current.DisplayAlert(AppResources.Oops, AppResources.LoginFailure, AppResources.OK);
                }
            });
        }
    }
}
