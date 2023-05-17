using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class LoginView : ContentPage
{
	public LoginView(LoginViewModel viewModel)
	{
        BindingContext = viewModel;
        InitializeComponent();
	}
	public LoginView() : this(Ioc.Default.GetRequiredService<LoginViewModel>())
	{
	}
}