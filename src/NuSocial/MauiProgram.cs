using CommunityToolkit.Maui.Markup;
using NuSocial.Core.Threading;
using UraniumUI;
using Microsoft.Extensions.Logging;
using NostrLib;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NuSocial;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldSuppressExceptionsInConverters(true);
                options.SetShouldSuppressExceptionsInBehaviors(false);
                options.SetShouldSuppressExceptionsInAnimations(false);
            })
            .UseMauiCommunityToolkitMarkup()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFontAwesomeIconFonts();
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var mauiDispatcher = new MauiDispatcher();
        var settingsService = new SettingsService();
        builder.Services.AddSingleton<ICustomDispatcher>(mauiDispatcher);
        builder.Services.AddSingleton<IDialogService>(new DialogService(mauiDispatcher));
        builder.Services.AddSingleton<IDatabase, LocalStorage>();

        builder.Services.AddSingleton<ISettingsService>(settingsService);
        builder.Services.AddSingleton<INostrClient, NostrClient>(x => new NostrClient(settingsService.GetId(), settingsService.GetRelays()));

        builder.Services.AddSingleton<ProfileViewModel>();

        builder.Services.AddSingleton<ProfilePage>();

        builder.Services.AddTransient<TimelineDetailViewModel>();
        builder.Services.AddTransient<TimelineDetailPage>();

        builder.Services.AddSingleton<TimelineViewModel>();
        builder.Services.AddSingleton<GlobalViewModel>();

        builder.Services.AddSingleton<TimelinePage>();
        builder.Services.AddSingleton<GlobalPage>();

        builder.Services.AddSingleton<LocalizationViewModel>();
        builder.Services.AddSingleton<LocalizationPage>();

        return builder.Build();
    }
}
