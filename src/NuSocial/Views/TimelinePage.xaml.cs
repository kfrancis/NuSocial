namespace NuSocial.Views;

public partial class TimelinePage : BetterUraniumContentPage<TimelineViewModel>
{
    public TimelinePage(TimelineViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        ViewModel?.LoadDataAsync();
    }
}