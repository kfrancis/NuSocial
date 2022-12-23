using CommunityToolkit.Maui.Markup;
using NuSocial.Core.Threading;
using UraniumUI;
using Microsoft.Extensions.Logging;

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

        builder.Services.AddSingleton<ICustomDispatcher>(mauiDispatcher);
        builder.Services.AddSingleton<IDialogService>(new DialogService(mauiDispatcher));
        builder.Services.AddSingleton<IDatabase, LocalStorage>();

        builder.Services.AddSingleton<ProfileViewModel>();

        builder.Services.AddSingleton<ProfilePage>();

        builder.Services.AddTransient<SampleDataService>();
        builder.Services.AddTransient<TimelineDetailViewModel>();
        builder.Services.AddTransient<TimelineDetailPage>();

        builder.Services.AddSingleton<TimelineViewModel>();

        builder.Services.AddSingleton<TimelinePage>();

        builder.Services.AddSingleton<LocalizationViewModel>();

        builder.Services.AddSingleton<LocalizationPage>();

        return builder.Build();
    }
}
