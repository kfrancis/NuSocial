using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class ListDetailPage : ContentPage
{
	ListDetailViewModel ViewModel;

	public ListDetailPage(ListDetailViewModel viewModel)
	{
		InitializeComponent();

		BindingContext = ViewModel = viewModel;
	}

    public ListDetailPage()
        : this(Ioc.Default.GetRequiredService<ListDetailViewModel>())
    {
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		await ViewModel.LoadDataAsync();
	}
}
