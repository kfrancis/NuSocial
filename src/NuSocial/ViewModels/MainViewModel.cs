using CommunityToolkit.Mvvm.Messaging;
using NuSocial.Core.ViewModel;
using NuSocial.Messages;
using System.Collections.Immutable;
using Volo.Abp.DependencyInjection;

namespace NuSocial.ViewModels;

public partial class MainViewModel : BaseViewModel, ITransientDependency
{
    [ObservableProperty]
    private ObservableCollection<Post> _posts = new();

    private List<Post> _postsWaiting = new();

    [ObservableProperty]
    private User? _user;

    public MainViewModel(IDialogService dialogService, INavigationService navigationService) : base(dialogService, navigationService)
    {
    }

    public string UnreadLabel => $"{L["Unread"]} ({_postsWaiting.Count})";

    public override Task OnFirstAppear()
    {
        WeakReferenceMessenger.Default.Send<ResetNavMessage>(new());
        WeakReferenceMessenger.Default.Register<NostrUserChangedMessage>(this, (r, m) =>
        {
            UpdateUser();
        });
        WeakReferenceMessenger.Default.Register<NostrPostMessage>(this, (r, m) =>
        {
            ReceivePost(m.Value);
        });
        return Task.CompletedTask;
    }

    public override Task OnParameterSet()
    {
        return SetBusyAsync(() =>
        {
            if (NavigationParameter is User user)
            {
                GlobalSetting.Instance.CurrentUser = user;
            }

            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task BoostPostAsync(Post post)
    {
        return SetBusyAsync(() =>
        {
            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task GoToDetailsAsync(Post item)
    {
        return SetBusyAsync(() =>
        {
            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task ReactToPostAsync()
    {
        return SetBusyAsync(() =>
        {
            return Task.CompletedTask;
        });
    }

    private void ReceivePost(string? value)
    {
        _postsWaiting.Add(new Post() { Content = value ?? string.Empty, CreatedAt = DateTime.Now });
        OnPropertyChanged(nameof(UnreadLabel));
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task RefreshAsync()
    {
        return SetBusyAsync(() =>
        {
            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task ReplyToPostAsync()
    {
        return SetBusyAsync(() =>
        {
            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task UpdatePostsAsync()
    {
        return SetBusyAsync(() =>
        {
            var posts = _postsWaiting.ToImmutableList();
            _postsWaiting.Clear();
            OnPropertyChanged(nameof(UnreadLabel));
            foreach (var post in posts)
            {
                Posts.AddFirst(post);
            }
            return Task.CompletedTask;
        });
    }

    private void UpdateUser()
    {
        throw new NotImplementedException();
    }
}