using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class WebViewPage : ContentPage
{
	public WebViewPage(WebViewViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
    public WebViewPage()
        : this(Ioc.Default.GetRequiredService<WebViewViewModel>())
    {
    }
}
