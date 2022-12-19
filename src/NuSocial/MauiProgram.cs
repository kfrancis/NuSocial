namespace NuSocial;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

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
