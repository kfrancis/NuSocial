using UraniumUI.Pages;

namespace NuSocial.Views;

public partial class TimelinePage : UraniumContentPage
{
    TimelineViewModel ViewModel;

    public TimelinePage(TimelineViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = ViewModel = viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        await ViewModel.LoadDataAsync();
    }
}
