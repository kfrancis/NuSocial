namespace NuSocial.Views;

public partial class CreateAccountPage : BetterUraniumContentPage<CreateAccountViewModel>
{
    public CreateAccountPage(CreateAccountViewModel vm)
    {
        InitializeComponent();
        ViewModel = vm;
    }
}