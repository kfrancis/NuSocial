using CommunityToolkit.Mvvm.DependencyInjection;
using NuSocial.Core;
using NuSocial.Managers;
using NuSocial.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Services
{
    public interface INavigationService
    {
        Task Initialize(Action? callback = null);

        Task NavigateTo(string key);

        Task NavigateTo(string key, object parameter);
    }

    /// <summary>
    /// Service that facilitates ViewModel level navigation
    /// </summary>
    public class NavigationService : INavigationService, ITransientDependency
    {
        private IAccessTokenManager? _accessTokenManager;
        private IApplicationContext? _applicationContext;
        private IDataStorageService? _dataStorageService;

        public NavigationService()
        {
            Current = this;
        }

        public static INavigationService Current { get; set; } = null!;
        public IAccessTokenManager AccessTokenManager => _accessTokenManager ??= Ioc.Default.GetRequiredService<IAccessTokenManager>();

        public IApplicationContext ApplicationContext => _applicationContext ??= Ioc.Default.GetRequiredService<IApplicationContext>();

        public IDataStorageService DataStorageService => _dataStorageService ??= Ioc.Default.GetRequiredService<IDataStorageService>();

        public async Task Initialize(Action? callback = null)
        {
            if (AccessTokenManager.IsUserLoggedIn)
            {
                await Shell.Current.GoToAsync("//main");
            }
            else
            {
                if (Config.IsFirstLogin)
                {
                    await Shell.Current.GoToAsync("//start");
                }
                else
                {
                    await Shell.Current.GoToAsync(nameof(LoginViewModel));
                }
            }

            if (callback != null)
                await callback.InvokeAsync();
        }

        public Task NavigateTo(string key)
        {
            return Shell.Current.GoToAsync(key);
        }

        public async Task NavigateTo(string key, object parameter)
        {
            if (parameter is IDictionary<string, object> dictionary)
            {
                await Shell.Current.GoToAsync(key, dictionary);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "cabParameter", parameter }
            };

            await Shell.Current.GoToAsync(key, parameters);
        }
    }
}
