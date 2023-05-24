using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Core.ViewModel;
using NuSocial.Localization;
using NuSocial.Messages;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class ShellViewModel : BaseViewModel, ISingletonDependency
{
    [ObservableProperty]
    private string _appVersion = string.Empty;

    private IAuthService? _authService;

    [ObservableProperty]
    private string _demoState = string.Empty;

    [ObservableProperty]
    private bool _isAdmin = false;

    [ObservableProperty]
    private bool _isPresented;

    private IRedirectService? _redirectService;

    public ShellViewModel(IAppInfo appInfo, LocalizationResourceManager localizationManager)
    {
        if (appInfo is null)
        {
            throw new ArgumentNullException(nameof(appInfo));
        }

        AppVersion = $"v{appInfo.Version.Major}.{appInfo.Version.Minor}";

        WeakReferenceMessenger.Default.Register<ResetNavMessage>(this, (r, m) =>
        {
            RefreshMenuItems(true);
        });

        WeakReferenceMessenger.Default.Register<LogoutMessage>(this, (r, m) =>
        {
            RefreshMenuItems(false);
        });

        localizationManager.PropertyChanged += (_, _) =>
        {
            UpdateProperties();
        };
    }

    private void UpdateProperties()
    {
        throw new NotImplementedException();
    }

    public ShellViewModel(LocalizationResourceManager localizationManager)
        : this(AppInfo.Current, localizationManager)
    {
    }

    /// <summary>
    /// Logs out the user
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    public Task LogoutPressedAsync()
    {
        return SetBusyAsync(async () =>
        {
            IsPresented = false;
            _authService ??= Ioc.Default.GetRequiredService<IAuthService>();
            await _authService.LogoutAsync();
        });
    }

    private void RefreshMenuItems(bool isLoggedIn)
    {
    }
}