using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels
{
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

        public ShellViewModel(IAppInfo appInfo)
        {
            if (appInfo is null)
            {
                throw new ArgumentNullException(nameof(appInfo));
            }

            AppVersion = $"v{appInfo.Version.Major}.{appInfo.Version.Minor}";

            WeakReferenceMessenger.Default.Register<DataLoadedMessage>(this, (r, m) =>
            {
                RefreshMenuItems(true);
            });

            WeakReferenceMessenger.Default.Register<LogoutMessage>(this, (r, m) =>
            {
                RefreshMenuItems(false);
            });
        }

        public ShellViewModel()
            : this(AppInfo.Current)
        {
        }

        /// <summary>
        /// Activates the Messages page
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public static void MessagesPressed()
        {
            Console.WriteLine("MessagesPressed");
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

        /// <summary>
        /// Activates the support page
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public Task SupportPressedAsync()
        {
            return SetBusyAsync(() =>
            {
                _redirectService ??= Ioc.Default.GetRequiredService<IRedirectService>();
                return _redirectService.OpenUrl(new Uri("https://nostr.com/"));
            });
        }

        private void RefreshMenuItems(bool isLoggedIn)
        {
          
        }
    }
}
