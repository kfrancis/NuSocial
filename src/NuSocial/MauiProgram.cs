using CommunityToolkit.Maui.Markup;
using NuSocial.Core.Threading;
using UraniumUI;
using Microsoft.Extensions.Logging;
using NostrLib;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using InputKit.Handlers;

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

        var mauiDispatcher = new MauiDispatcher();

        builder.Services.AddSingleton<ICustomDispatcher>(mauiDispatcher);
        builder.Services.AddSingleton<IDialogService>(new DialogService(mauiDispatcher));
        var db = new LocalStorage();
        builder.Services.AddSingleton<IDatabase>(db);
        var settingsService = new SettingsService(db);
        builder.Services.AddSingleton<ISettingsService>(settingsService);
        var nostrClient = new NostrClient(settingsService.GetId(), settingsService.GetRelays());
        builder.Services.AddSingleton<INostrClient, NostrClient>(x => nostrClient);
        builder.Services.AddSingleton<IAuthorService>(new AuthorService(nostrClient));

        builder.Services.AddSingleton<ProfileViewModel>();

        builder.Services.AddSingleton<ProfilePage>();

        builder.Services.AddTransient<TimelineDetailViewModel>();
        builder.Services.AddTransient<TimelineDetailPage>();

        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddSingleton<CreateAccountViewModel>();
        builder.Services.AddSingleton<StartViewModel>();
        builder.Services.AddSingleton<TimelineViewModel>();
        builder.Services.AddSingleton<GlobalViewModel>();

        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<CreateAccountPage>();
        builder.Services.AddSingleton<StartPage>();
        builder.Services.AddSingleton<TimelinePage>();
        builder.Services.AddSingleton<GlobalPage>();

        builder.Services.AddSingleton<LocalizationViewModel>();
        builder.Services.AddSingleton<LocalizationPage>();

        return builder.Build();
    }
}
