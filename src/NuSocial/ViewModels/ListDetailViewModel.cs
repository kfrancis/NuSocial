using CommunityToolkit.Mvvm.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class ListDetailViewModel : BaseViewModel, ITransientDependency
{
	readonly SampleDataService dataService;

	[ObservableProperty]
	bool isRefreshing;

	[ObservableProperty]
	ObservableCollection<SampleItem> items;

	public ListDetailViewModel(SampleDataService service)
	{
		dataService = service;
	}

    public ListDetailViewModel() : this(Ioc.Default.GetRequiredService<SampleDataService>())
    {
    }

    [RelayCommand]
	private async void OnRefreshing()
	{
		IsRefreshing = true;

		try
		{
			await LoadDataAsync();
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	[RelayCommand]
	public async Task LoadMore()
	{
		var items = await dataService.GetItems();

		foreach (var item in items)
		{
			Items.Add(item);
		}
	}

	public async Task LoadDataAsync()
	{
		Items = new ObservableCollection<SampleItem>(await dataService.GetItems());
	}

	[RelayCommand]
	private async void GoToDetails(SampleItem item)
	{
		await Shell.Current.GoToAsync(nameof(ListDetailDetailPage), true, new Dictionary<string, object>
		{
			{ "Item", item }
		});
	}
}
