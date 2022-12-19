using UraniumUI.Pages;

namespace NuSocial.Views;

public partial class ProfilePage : UraniumContentPage
{
	public ProfilePage(ProfileViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
