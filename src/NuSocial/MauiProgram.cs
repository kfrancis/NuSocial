using Autofac;
using Autofac.Diagnostics;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Mopups.Hosting;
using NuSocial.Core.Threading;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.FastConsole;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Reflection;
using Volo.Abp;
using Volo.Abp.Autofac;

#if WINDOWS10_0_17763_0_OR_GREATER
using NuSocial.Platforms.Windows;
#endif

namespace NuSocial;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        SetupSerilog();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldSuppressExceptionsInConverters(true);
                options.SetShouldSuppressExceptionsInBehaviors(false);
                options.SetShouldSuppressExceptionsInAnimations(false);
            })
            .UseSkiaSharp()
            .UseMauiCommunityToolkitMarkup()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("FontAwesome6FreeBrands.otf", "FontAwesomeBrands");
                fonts.AddFont("FontAwesome6FreeRegular.otf", "FontAwesomeRegular");
                fonts.AddFont("FontAwesome6FreeSolid.otf", "FontAwesomeSolid");
                fonts.AddFont("Urbanist-Black.ttf", "Black");
                fonts.AddFont("Urbanist-BlackItalic.ttf", "BlackItalic");
                fonts.AddFont("Urbanist-Bold.ttf", "Bold");
                fonts.AddFont("Urbanist-BoldItalic.ttf", "BoldItalic");
                fonts.AddFont("Urbanist-ExtraBold.ttf", "ExtraBold");
                fonts.AddFont("Urbanist-ExtraBoldItalic.ttf", "ExtraBoldItalic");
                fonts.AddFont("Urbanist-ExtraLight.ttf", "ExtraLight");
                fonts.AddFont("Urbanist-ExtraLightItalic.ttf", "ExtraLightItalic");
                fonts.AddFont("Urbanist-Italic.ttf", "Italic");
                fonts.AddFont("Urbanist-Light.ttf", "Light");
                fonts.AddFont("Urbanist-LightItalic.ttf", "LightItalic");
                fonts.AddFont("Urbanist-Medium.ttf", "Medium");
                fonts.AddFont("Urbanist-MediumItalic.ttf", "MediumItalic");
                fonts.AddFont("Urbanist-Regular.ttf", "Regular");
                fonts.AddFont("Urbanist-SemiBold.ttf", "SemiBold");
                fonts.AddFont("Urbanist-SemiBoldItalic.ttf", "SemiBoldItalic");
                fonts.AddFont("Urbanist-Thin.ttf", "Thin");
                fonts.AddFont("Urbanist-ThinItalic.ttf", "ThinItalic");
            })
            .ConfigureMopups()
            .ConfigureContainer<ContainerBuilder>(new AbpAutofacServiceProviderFactory(GetAutofacContainerBuilder(builder.Services)));

        ConfigureFromConfigurationOptions(builder);

        builder.Services.AddApplication<NuSocialModule>(options =>
        {
            options.Services.ReplaceConfiguration(builder.Configuration);
        });

        AddDebugLogging(builder.Logging);

        // **** THIS SECTION IS WHAT IS RELEVANT FOR YOU ************ //
        builder.ConfigureLifecycleEvents(events =>
        {
#if WINDOWS10_0_17763_0_OR_GREATER
            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnWindowCreated(window =>
                {
                    window.TryMicaOrAcrylic();
                });
            });
#endif
        });
        // ************* END OF RELEVANT SECTION *********** //

        var app = builder.Build();

        app.Services.GetRequiredService<IAbpApplicationWithExternalServiceProvider>().Initialize(app.Services);

        return app;
    }

    private static void SetupSerilog()
    {
        var flushInterval = new TimeSpan(0, 0, 1);
        var file = Path.Combine(FileSystem.AppDataDirectory, "NuSocial.log");

        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        //.WriteTo.File(file, flushToDiskInterval: flushInterval, encoding: System.Text.Encoding.UTF8, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 22)
        .WriteTo.FastConsole()
        .CreateLogger();
    }

    [Conditional("DEBUG")]
    private static void AddDebugLogging(ILoggingBuilder logging)
    {
        logging.AddDebug();

        //AppDomain.CurrentDomain.FirstChanceException += ExceptionService.Set;
        //AppDomain.CurrentDomain.UnhandledException += ExceptionService.Set;
        //TaskScheduler.UnobservedTaskException += ExceptionService.Set;
    }

    private static void ConfigureFromConfigurationOptions(MauiAppBuilder builder)
    {
        var localType = Assembly.GetExecutingAssembly().DefinedTypes.Where(type => type.Name.EndsWith("MauiProgram", StringComparison.Ordinal)).First();
        var assembly = localType.Assembly;
        var names = assembly.GetManifestResourceNames().ToList();
        using var stream = assembly.GetManifestResourceStream(names.Find(x => x.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase)) ?? "appsettings.json");

        if (stream != null)
        {
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            builder.Configuration.AddConfiguration(config);
        }

        using var stream2 = assembly.GetManifestResourceStream(names.Find(x => x.EndsWith("appsettings.Development.json", StringComparison.OrdinalIgnoreCase)) ?? "appsettings.Development.json");
        if (stream2 != null)
        {
            var config = new ConfigurationBuilder().AddJsonStream(stream2).Build();
            builder.Configuration.AddConfiguration(config);
        }
    }

    private static ContainerBuilder GetAutofacContainerBuilder(IServiceCollection services)
    {
        var db = new LocalStorage();
        services.AddSingleton<IDatabase>(db);
        services.AddSingleton<ICustomDispatcher, MauiDispatcher>();
        
#if DEBUG
        services.AddSingleton<INostrService>(new TestNostrService());
#else
        services.AddSingleton<INostrService>(new NostrService(db));
#endif
        services.AddLocalization();
        services.AddLogging(logging => logging.AddSerilog());

        var builder = new Autofac.ContainerBuilder();

        SetupAutofacDebug(builder);

        return builder;
    }

    [Conditional("DEBUG")]
    private static void SetupAutofacDebug(ContainerBuilder builder)
    {
        var tracer = new DefaultDiagnosticTracer();
        tracer.OperationCompleted += (sender, args) =>
        {
            if (args.OperationSucceeded == false)
                Console.WriteLine(args.TraceContent);
        };

        builder.RegisterBuildCallback(c =>
        {
            if (c is Autofac.IContainer container)
            {
                container.SubscribeToDiagnostics(tracer);
            }
        });
    }
}
