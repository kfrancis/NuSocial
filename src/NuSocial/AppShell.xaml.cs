using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(TimelineDetailPage), typeof(TimelineDetailPage));
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
