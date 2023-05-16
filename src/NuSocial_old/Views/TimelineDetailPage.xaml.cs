using UraniumUI.Pages;

namespace NuSocial.Views;

public partial class TimelineDetailPage : UraniumContentPage
{
    public TimelineDetailPage(TimelineDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
