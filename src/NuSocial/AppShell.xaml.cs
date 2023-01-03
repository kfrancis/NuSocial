using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(TimelineDetailPage), typeof(TimelineDetailPage));
        themeSwitch.IsToggled = App.Current.RequestedTheme == AppTheme.Dark;
        App.Current.RequestedThemeChanged += (s, e) =>
        {
            themeSwitch.IsToggled = App.Current.RequestedTheme == AppTheme.Dark;
        };
    }

    private void ThemeToggled(object sender, ToggledEventArgs e)
    {
        App.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is BaseViewModel bindingViewContext)
        {
            bindingViewContext.OnAppearing();
        }
    }

    protected override async void OnDisappearing()
    {
        if (BindingContext is BaseViewModel bindingViewContext)
        {
            bindingViewContext.OnDisappearing();
        }

        base.OnDisappearing();
    }
}
