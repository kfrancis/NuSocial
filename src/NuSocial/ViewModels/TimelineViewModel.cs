namespace NuSocial.ViewModels;

public partial class TimelineViewModel : BaseViewModel
{
    private readonly SampleDataService dataService;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private ObservableCollection<SampleItem> items;

    public TimelineViewModel(SampleDataService service)
    {
        dataService = service;
    }

    public async Task LoadDataAsync()
    {
        Items = new ObservableCollection<SampleItem>(await dataService.GetItems());
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

    [RelayCommand]
    private async void GoToDetails(SampleItem item)
    {
        await Shell.Current.GoToAsync(nameof(TimelineDetailPage), true, new Dictionary<string, object>
        {
            { "Item", item }
        });
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
}