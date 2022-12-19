namespace NuSocial.Views;

public partial class TimelineDetailPage : ContentPage
{
    public TimelineDetailPage(TimelineDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
