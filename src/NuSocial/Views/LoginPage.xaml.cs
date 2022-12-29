namespace NuSocial.Views;

public partial class LoginPage : BetterUraniumContentPage<LoginViewModel>
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();

        ViewModel = vm;
    }
}
