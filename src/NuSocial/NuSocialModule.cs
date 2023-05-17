using CommunityToolkit.Maui.Layouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuSocial.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Autofac;
using Volo.Abp.Http.Client;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace NuSocial
{
    [DependsOn(
        typeof(AbpLocalizationModule),
        typeof(AbpAutofacModule))]
    public class NuSocialModule : AbpModule
    {
        public NuSocialModule()
        {
            SkipAutoServiceRegistration = true;
        }

        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            SetupDebugProxy();
        }

        [Conditional("DEBUG")]
        private void SetupDebugProxy()
        {
            PreConfigure<AbpHttpClientBuilderOptions>(options =>
            {
                options.ProxyClientBuildActions.Add((_, clientBuilder) =>
                {
                    clientBuilder.ConfigurePrimaryHttpMessageHandler(GetInsecureHandler);
                });
            });
        }

        //https://docs.microsoft.com/en-us/xamarin/cross-platform/deploy-test/connect-to-local-web-services#bypass-the-certificate-security-check
        private static HttpMessageHandler GetInsecureHandler()
        {
#if ANDROID
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            if (cert.Issuer.Equals("CN=localhost"))
            {
                return true;
            }

            return errors == System.Net.Security.SslPolicyErrors.None;
        };
        return handler;
#elif IOS
        var handler = new NSUrlSessionHandler
        {
            TrustOverrideForUrl = (sender, url, trust) => url.StartsWith("https://localhost", StringComparison.InvariantCultureIgnoreCase)
        };
        return handler;
#elif WINDOWS || MACCATALYST
            return new HttpClientHandler();
#else
         throw new PlatformNotSupportedException("Only Android, iOS, MacCatalyst, and Windows supported.");
#endif
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.AddEmbedded<NuSocialModule>();
            });

            Configure<AbpLocalizationOptions>(options =>
            {
                options.Resources
                    .Add<NuSocialResource>("en")
                    .AddBaseTypes(typeof(AbpValidationResource))
                    .AddVirtualJson("/Localization/NuSocial");

                options.DefaultResourceType = typeof(NuSocialResource);
            });

            Configure<AbpExceptionLocalizationOptions>(options =>
            {
                options.MapCodeNamespace("NuSocial", typeof(NuSocialResource));
            });

            context.Services.AddAssemblyOf<ShellViewModel>();

            var configuration = context.Services.GetConfiguration();

            ConfigureLocalization();
            ConfigureAnalytics(context.Services, configuration);
        }

        private void ConfigureLocalization()
        {
            Configure<AbpLocalizationOptions>(options =>
            {
                options.Languages.Add(new LanguageInfo("en", "en", "English"));
                //options.Languages.Add(new LanguageInfo("fr", "fr", "Français", "fr"));
            });
        }

        private static void ConfigureAnalytics(IServiceCollection services, IConfiguration config)
        {
            if (Guid.TryParse(config["Analytics:AppCenterKey"], out var key) && !Debugger.IsAttached)
            {
                _ = services.AddSingleton<IAnalyticsService>(new AppCenterService(key.ToString()));
            }
            else
            {
                _ = services.AddSingleton<IAnalyticsService>(new DebugAnalyticsService());
            }
        }
    }


}
