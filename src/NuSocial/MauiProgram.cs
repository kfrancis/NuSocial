using CommunityToolkit.Maui.Markup;
using NuSocial.Core.Threading;
using UraniumUI;
using Microsoft.Extensions.Logging;
using NostrLib;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using InputKit.Handlers;
using Microsoft.Maui.LifecycleEvents;

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
            })
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddUraniumUIHandlers();
                handlers.AddInputKitHandlers();
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        SetupServices(builder);
        SetupPages(builder);

        return builder.Build();
    }

    private static void SetupPages(MauiAppBuilder builder)
    {
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<ProfilePage>();

        builder.Services.AddTransient<TimelineDetailViewModel>();
        builder.Services.AddTransient<TimelineDetailPage>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();

        builder.Services.AddTransient<CreateAccountViewModel>();
        builder.Services.AddTransient<CreateAccountPage>();

        builder.Services.AddTransient<StartViewModel>();
        builder.Services.AddTransient<StartPage>();

        builder.Services.AddTransient<TimelineViewModel>();
        builder.Services.AddTransient<TimelinePage>();

        builder.Services.AddTransient<GlobalViewModel>();
        builder.Services.AddTransient<GlobalPage>();
    }

    private static void SetupServices(MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<ICustomDispatcher, MauiDispatcher>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IDatabase, LocalStorage>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<INostrClient, NostrClient>(x =>
        {
            var settings = x.GetRequiredService<ISettingsService>();
            return new NostrClient(settings.GetId(), false, settings.GetRelays());
        });
        builder.Services.AddSingleton<IAuthorService, AuthorService>();
    }
}
