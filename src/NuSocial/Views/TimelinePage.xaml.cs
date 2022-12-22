namespace NuSocial.Views;

public partial class TimelinePage : BetterUraniumContentPage<TimelineViewModel>
{
    public TimelinePage(TimelineViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
    }
}