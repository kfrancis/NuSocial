using Android.App;
using Android.Content.PM;
using Android.OS;

namespace NuSocial;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var oldEx = e.ExceptionObject as Exception;
        var newExc = new Exception("CurrentDomain_UnhandledException", oldEx?.Demystify());
        LogUnhandledException(newExc);
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var oldEx = e.Exception;
        var newExc = new Exception("TaskScheduler_UnobservedTaskException", oldEx?.Demystify());
        LogUnhandledException(newExc);
    }

    /// <summary>
    /// If there is an unhandled exception, the exception information is diplayed
    /// on screen the next time the app is started (only in debug configuration)
    /// </summary>
    [System.Diagnostics.Conditional("DEBUG")]
    private static async void DisplayCrashReport()
    {
        const string errorFilename = "Fatal.log";
        var libraryPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        var errorFilePath = Path.Combine(libraryPath, errorFilename);

        if (!File.Exists(errorFilePath))
        {
            return;
        }

        var errorText = File.ReadAllText(errorFilePath);

        if (App.Current?.MainPage != null)
        {
            var result = await App.Current.MainPage.DisplayAlert("Crash Report", errorText, "Clear", "Close", FlowDirection.MatchParent);
            if (result)
            {
                File.Delete(errorFilePath);
            }
        }
    }

    internal static void LogUnhandledException(Exception exception)
    {
        try
        {
            const string errorFileName = "Fatal.log";
            var libraryPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal); // iOS: Environment.SpecialFolder.Resources
            var errorFilePath = Path.Combine(libraryPath, errorFileName);
            var errorMessage = string.Format(CultureInfo.InvariantCulture, "Time: {0}\r\nError: Unhandled Exception\r\n{1}", DateTime.Now, exception.ToString());
            File.WriteAllText(errorFilePath, errorMessage);

            // Log to Android Device Logging.
            Android.Util.Log.Error("Crash Report", errorMessage);
        }
        catch
        {
            // just suppress any error logging exceptions
        }
    }
}
