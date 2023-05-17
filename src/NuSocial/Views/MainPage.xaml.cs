using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public MainPage()
		:this(Ioc.Default.GetRequiredService<MainViewModel>())
	{
	}
}
