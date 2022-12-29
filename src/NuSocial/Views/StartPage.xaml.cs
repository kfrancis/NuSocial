namespace NuSocial.Views;

public partial class StartPage : BetterUraniumContentPage<StartViewModel>
{
    public StartPage(StartViewModel vm)
    {
        InitializeComponent();

        ViewModel = vm;
    }
}
