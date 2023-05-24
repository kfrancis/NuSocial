using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Core.ViewModel;
using NuSocial.Messages;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class RelayViewTypeOption
{
    public Color BackgroundColour { get; set; } = Colors.Transparent;

    public string Name { get; set; } = string.Empty;
    public RelayViewType ViewType { get; set; }
}

public partial class RelaysViewModel : BaseViewModel, ITransientDependency
{
    private readonly IDatabase _db;

    [ObservableProperty]
    private ObservableCollection<RelayViewTypeOption> _options = new();

    [ObservableProperty]
    private ObservableCollection<Relay> _relays = new();

    [ObservableProperty]
    private RelayViewType _viewType = RelayViewType.My;

    public RelaysViewModel(IDialogService dialogService,
                                           INavigationService navigationService,
                           IDatabase db)
        : base(dialogService, navigationService)
    {
        Title = L["Relays"];
        _db = db;
    }

    public override async Task InitializeAsync()
    {
        // We need to cancel any work currently handling nostr information, so fire that message.
        WeakReferenceMessenger.Default.Send<NostrStateChangeMessage>(new(false));

        Relays = await _db.GetRelaysAsync();
        SetOptions(ViewType);
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task ChangeViewTypeAsync(RelayViewTypeOption viewType)
    {
        return SetBusyAsync(() =>
        {
            SetOptions(viewType.ViewType); return Task.CompletedTask;
        });
    }

    private void SetOptions(RelayViewType viewType)
    {
        Title = viewType switch
        {
            RelayViewType.My => L["My"],
            RelayViewType.Mutual => L["Mutual"],
            _ => L["Unknown"],
        };

        Options.Clear();
        Enum.GetNames<RelayViewType>().ToList().ForEach(x => Options.AddLast(new RelayViewTypeOption() { Name = L[$"Enum_{x}"], ViewType = (RelayViewType)Enum.Parse(typeof(RelayViewType), x) }));
    }
}