using CommunityToolkit.Mvvm.DependencyInjection;
using NuSocial.Core;

namespace NuSocial;

public partial class App : AppBase
{
	public App(IServiceProvider serviceProvider, AppShell shell) : base(serviceProvider)
    {
		InitializeComponent();

        MauiExceptions.UnhandledException += MauiExceptions_UnhandledException; ;

        MainPage = shell;
	}

    private void MauiExceptions_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e != null && e.ExceptionObject != null && e.ExceptionObject is Exception ex)
            Debug.WriteLine($"********************************** UNHANDLED EXCEPTION! Details: {ex.ToStringDemystified()}");
    }

    protected override async void OnStart()
    {
        base.OnStart();

        var nav = Ioc.Default.GetRequiredService<INavigationService>();
        await nav.Initialize();

        OnResume();
    }
}
