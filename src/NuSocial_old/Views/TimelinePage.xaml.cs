using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Markup;

namespace NuSocial.Views;

public partial class TimelinePage : BetterUraniumContentPage<TimelineViewModel>
{
    public TimelinePage(TimelineViewModel viewModel)
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
