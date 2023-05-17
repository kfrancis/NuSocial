using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Services
{
    public interface IRedirectService
    {
        Task OpenUrl(string url, bool appendPackageName = false);

        Task OpenUrl(Uri uri, bool appendPackageName = false);
    }

    public class RedirectService : IRedirectService, ITransientDependency
    {
        public Task OpenUrl(string url, bool appendPackageName = false)
        {
            return OpenUrl(new Uri(url), appendPackageName);
        }

        public Task OpenUrl(Uri uri, bool appendPackageName = false)
        {
            try
            {
                return Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                // An unexpected error occured. No browser may be installed on the device.
                var analytics = Ioc.Default.GetService<IAnalyticsService>();
                analytics?.Log(ex, (nameof(uri), uri), (nameof(appendPackageName), appendPackageName));
                return Task.CompletedTask;
            }
        }
    }
}
