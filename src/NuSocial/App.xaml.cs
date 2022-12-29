using CommunityToolkit.Mvvm.DependencyInjection;
using NuSocial.Core.Exceptions;

namespace NuSocial;

public partial class App : AppBase
{
    public App(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        InitializeComponent();

        MauiExceptions.UnhandledException += MauiExceptions_UnhandledException;

        // Uncomment the following as a quick way to test loading resources for different languages
        // CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("es");
    }

    private void MauiExceptions_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e != null && e.ExceptionObject != null && e.ExceptionObject is Exception ex)
            System.Diagnostics.Debug.WriteLine($"********************************** UNHANDLED EXCEPTION! Details: {ex.ToStringDemystified()}");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        MainPage = new AppShell();
        return base.CreateWindow(activationState);
    }
}

public abstract class AppBase : Application
{
    private static Action? _exitAction;

    protected AppBase(IServiceProvider services)
    {
        Ioc.Default.ConfigureServices(services);
    }

    public static Task OnSessionTimeout()
    {
        return Task.CompletedTask;
        //return Ioc.Default.GetRequiredService<IAuthService>().LogoutAsync();
    }

    public static Task OnAccessTokenRefresh(string newAccessToken)
    {
        return Task.CompletedTask;
        //await Ioc.Default.GetRequiredService<IDataStorageService>().StoreAccessTokenAsync(newAccessToken);
    }

    public static Action? ExitApplication
    {
        get
        {
            return _exitAction;
        }
        set
        {
            _exitAction = value ?? new Action(DoNothing);
        }
    }

    private static void DoNothing()
    {
        // Nothing
    }

    public static void LoadPersistedSession()
    {
        //var accessTokenManager = Ioc.Default.GetRequiredService<IAccessTokenManager>();
        //var dataStorageService = Ioc.Default.GetRequiredService<IDataStorageService>();
        //var applicationContext = Ioc.Default.GetRequiredService<IApplicationContext>();

        //accessTokenManager.AuthenticateResult = await dataStorageService.RetrieveAuthenticateResult();
        //applicationContext.Load(await dataStorageService.RetrieveLoginInfo());
    }
}
