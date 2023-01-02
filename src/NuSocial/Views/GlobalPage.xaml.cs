using CommunityToolkit.Maui.Extensions;
using UraniumUI.Pages;

namespace NuSocial.Views;

public partial class GlobalPage : BetterUraniumContentPage<GlobalViewModel>
{
    public GlobalPage(GlobalViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
    }

    private async void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is Image img)
        {
            await img.BackgroundColorTo(Colors.Gray);
        }
    }

    private async void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        if (sender is Image img)
        {
            await img.BackgroundColorTo(Colors.LightGray);
        }
    }
}
