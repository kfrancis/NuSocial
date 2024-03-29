﻿using CommunityToolkit.Mvvm.Messaging;
using Mopups.Services;
using NBitcoin;
using Nostr.Client.Messages;
using Nostr.Client.Messages.Metadata;
using Nostr.Client.Requests;
using NuSocial.Core;
using NuSocial.Core.ViewModel;
using NuSocial.Messages;
using System.Collections.Immutable;
using Volo.Abp.DependencyInjection;
using Contact = NuSocial.Models.Contact;

namespace NuSocial.ViewModels;

public partial class MainViewModel : BaseViewModel, ITransientDependency, IDisposable
{
    private readonly IAuthorService _authorService;
    private readonly INostrService _nostrService;
    private CancellationTokenSource? _cts = new();
    private const int _postThreshold = 100;

    [ObservableProperty]
    private ObservableCollection<Post> _posts = new();

    private List<Post> _postsWaiting = new();

    [ObservableProperty]
    private User? _user;
    private bool _isDisposed;

    public MainViewModel(IDialogService dialogService,
                         INavigationService navigationService,
                         IAuthorService authorService,
                         INostrService nostrService)
        : base(dialogService, navigationService)
    {
        _authorService = authorService;
        _nostrService = nostrService;
        User = GlobalSetting.Instance.CurrentUser;
    }

    public string UnreadLabel => $"{L["Unread"]} ({_postsWaiting.Count})";

    public override async Task OnFirstAppear()
    {

        
    }


    public override async Task OnAppearing()
    {

        WeakReferenceMessenger.Default.Send<ResetNavMessage>(new());

        WeakReferenceMessenger.Default.Register<NostrUserChangedMessage>(this, (r, m) =>
        {
            UpdateUser(m.Value.privKey, m.Value.pubKey);
        });

        WeakReferenceMessenger.Default.Register<NostrStateChangeMessage>(this, (r, m) =>
        {
            // Should we receive the state change message, cancel the nostr token to let other tasks continue.
            _cts?.Cancel();
        });

        WeakReferenceMessenger.Default.Register<NostrReadyMessage>(this, (r, m) =>
        {
            _ = Task.Run(async () => {
                if (User != null && User.PublicKey != null)
                {
                    _cts ??= new();
                    var userProfile = await _nostrService.GetProfileAsync(User.PublicKey.Hex, true, _cts.Token);
                    string t = "";

                    WeakReferenceMessenger.Default.Register<NostrPostMessage>(this, (r, m) =>
                    {
                        ReceivePost(m.Value);
                    });
                }
            });
        });

        //if (GlobalSetting.Instance.DemoMode && _nostrService is TestNostrService)
        //{
        _cts ??= new();
        await _nostrService.StartNostrAsync(_cts.Token);
        //}
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
    private Task ShowPopupAsync()
    {
        return SetBusyAsync(() =>
        {
            //MopupService.Instance.PushAsync(new SendPostPopup())
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

    public override Task OnDisappearing()
    {
        _cts?.Cancel();
        return base.OnDisappearing();
    }

    private async void ReceivePost(NostrEvent? ev)
    {
        if (ev == null) return;
        if (string.IsNullOrEmpty(ev.Pubkey)) return;
        _cts ??= new();

        var author = await _authorService.GetInfoAsync(ev.Pubkey, _cts.Token);
        var contact = new Contact() { PublicKey = ev.Pubkey, PetName = author.DisplayName ?? author.Name ?? string.Empty };
        if (!string.IsNullOrEmpty(author.Picture))
        {
            contact.Picture = new Picture(new Uri(author.Picture));
        }
        contact.Nip05 = author.Nip05;
        if (!string.IsNullOrEmpty(contact.Nip05) && !string.IsNullOrEmpty(author.DisplayName) && contact.Nip05.StartsWith(author.DisplayName, StringComparison.OrdinalIgnoreCase))
        {
            contact.Nip05 = contact.Nip05.Replace(author.DisplayName, string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        var post = new Post()
        {
            Content = ev.Content ?? string.Empty,
            CreatedAt = ev.CreatedAt ?? DateTime.Now,
            Contact = contact
        };
        _postsWaiting.Add(post);
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

            if (Posts.Count > _postThreshold)
            {
                Posts.RemoveLastN(posts.Count);
            }
            foreach (var post in posts)
            {
                Posts.AddFirst(post);
            }
            return Task.CompletedTask;
        });
    }

    private void UpdateUser(string privKey, string pubKey)
    {

    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _cts?.Dispose();
            }

            _isDisposed = true;
        }
    }

    ~MainViewModel()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}