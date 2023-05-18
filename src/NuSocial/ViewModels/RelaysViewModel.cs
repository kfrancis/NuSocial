using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class RelaysViewModel : BaseViewModel, ITransientDependency
{
    public RelaysViewModel(IDialogService dialogService,
                           INavigationService navigationService,
                           IDatabase db) 
        : base(dialogService, navigationService)
    {
        Title = L["Relays"];
        _db = db;
    }

    [ObservableProperty]
    private ObservableCollection<Relay> _relays = new();

    private readonly IDatabase _db;

    public override async Task InitializeAsync()
    {
        Relays = await _db.GetRelaysAsync();
    }
}