using Foundation;
using SQLitePCL;
using System.Threading.Tasks;

namespace NuSocial;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp()
	{
        raw.SetProvider(new SQLite3Provider_sqlite3());

        SetExitAction();

        return MauiProgram.CreateMauiApp();
	}

    private static void SetExitAction()
    {
        AppBase.ExitApplication = Thread.CurrentThread.Abort;
    }
}
