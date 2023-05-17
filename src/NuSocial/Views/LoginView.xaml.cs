using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class LoginView
{
    public LoginView(LoginViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
    public LoginView() : this(Ioc.Default.GetRequiredService<LoginViewModel>())
    {
    }
}