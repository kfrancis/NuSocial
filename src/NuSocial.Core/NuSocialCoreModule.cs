using Microsoft.Extensions.Configuration;
using NuSocial.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;
using AppCenterService = NuSocial.Services.AppCenterService;

namespace NuSocial.Core
{
    [DependsOn(
        typeof(AbpLocalizationModule))]
    public class NuSocialCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.AddEmbedded<NuSocialCoreModule>("NuSocial");
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

            var configuration = context.Services.GetConfiguration();
            ConfigureAnalytics(context.Services, configuration);
        }

        private static void ConfigureAnalytics(IServiceCollection services, IConfiguration config)
        {
            //if (Guid.TryParse(config["Analytics:AppCenterKey"], out var key) && !Debugger.IsAttached)
            //{
            //    _ = services.AddSingleton<IAnalyticsService>(new AppCenterService(key.ToString()));
            //}
            //else
            //{
            //    _ = services.AddSingleton<IAnalyticsService>(new DebugAnalyticsService());
            //}
        }
    }
}