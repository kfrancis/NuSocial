using UraniumUI.Pages;

namespace NuSocial.Views;

public partial class GlobalPage : BetterUraniumContentPage<GlobalViewModel>
{
	public GlobalPage(GlobalViewModel viewModel)
	{
		InitializeComponent();

		ViewModel = viewModel;
	}
}