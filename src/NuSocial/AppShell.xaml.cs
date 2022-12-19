namespace NuSocial;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        Routing.RegisterRoute(nameof(TimelineDetailPage), typeof(TimelineDetailPage));
	}
}
