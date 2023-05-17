using CommunityToolkit.Mvvm.DependencyInjection;

namespace NuSocial.Views;

public partial class ListDetailDetailPage : ContentPage
{
	public ListDetailDetailPage(ListDetailDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    public ListDetailDetailPage()
        : this(Ioc.Default.GetRequiredService<ListDetailDetailViewModel>())
    {
    }
}
