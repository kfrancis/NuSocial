using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Graphics;
using NuSocial.Core.ViewModel;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class MessageViewModel : BaseViewModel, ITransientDependency
{
    [ObservableProperty]
    private ObservableCollection<MessageData> _messages = new ObservableCollection<MessageData>();

    [ObservableProperty]
    private Message? _parentMessage;

    public MessageViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }

    [ObservableProperty]
    private string? _searchText;

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task Search()
    {
        return SetBusyAsync(() =>
        {
            if (Messages != null && !string.IsNullOrEmpty(SearchText))
            {
                Messages = Messages.Where(x => x.Text.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToObservableCollection();
            }

            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task SearchTextChanged()
    {
        return SetBusyAsync(() =>
        {
            if (string.IsNullOrEmpty(SearchText) && ParentMessage != null)
            {
                if (ParentMessage.From != null)
                {
                    Title = ParentMessage.From.Name.ToString();
                }

                if (ParentMessage.Messages.Count > 0)
                {
                    Messages.AddIfNotContains(ParentMessage.Messages.OrderBy(x => x.When));
                }
            }
            return Task.CompletedTask;
        });
    }

    public override Task OnParameterSet()
    {
        return SetBusyAsync(() =>
        {
            if (NavigationParameter is Message parentMessage)
            {
                ParentMessage = parentMessage;

                if (parentMessage.From != null)
                {
                    Title = parentMessage.From.Name.ToString();
                }

                if (parentMessage.Messages.Count > 0)
                {
                    Messages.AddIfNotContains(parentMessage.Messages.OrderBy(x => x.When));
                }
            }
            return Task.CompletedTask;
        });
    }
}