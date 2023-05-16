namespace NuSocial.ViewModels;

[QueryProperty(nameof(Item), "Item")]
public partial class TimelineDetailViewModel : BaseViewModel
{
    [ObservableProperty]
    private Post item;
}