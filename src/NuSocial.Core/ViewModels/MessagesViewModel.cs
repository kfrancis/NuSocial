using Bogus;
using CommunityToolkit.Maui.Core.Extensions;
using Volo.Abp.DependencyInjection;
using Contact = NuSocial.Models.Contact;

namespace NuSocial.ViewModels;

public partial class MessagesViewModel : BaseViewModel, ITransientDependency
{
    private readonly IDatabase _db;

    [ObservableProperty]
    private ObservableCollection<Message>? _messages = new ObservableCollection<Message>();

    public MessagesViewModel(IDialogService dialogService,
                             INavigationService navigationService,
                             IDatabase db)
        : base(dialogService, navigationService)
    {
        _db = db;
    }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task Search()
    {
        return SetBusyAsync(() =>
        {
            if (Messages != null && !string.IsNullOrEmpty(SearchText))
            {
                Messages = Messages.Where(x => x.ContainsText(SearchText) == true).ToObservableCollection();
            }

            return Task.CompletedTask;
        });
    }
    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task Refresh()
    {
        return SetBusyAsync(() =>
        {
            LoadData();

            return Task.CompletedTask;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task SearchTextChanged()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            LoadData();
        }
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private Task ViewMessage(Message message)
    {
        return SetBusyAsync(() =>
        {
            return Navigation.NavigateTo(nameof(MessageViewModel), message);
        });
    }

    public override async Task InitializeAsync()
    {
        Title = L["Messages"];

        // Load data
        var messages = await _db.GetMessagesAsync();

        if (messages == null || !messages.Any())
        {
            var faker = new Faker();

            var nameFaker = new Faker<Name>()
                .RuleFor(n => n.First, f => f.Person.FirstName)
                .RuleFor(n => n.Last, f => f.Person.LastName);

            var pictureFaker = new Faker<Picture>()
                .RuleFor(p => p.Url, f => new(f.Internet.Avatar()));

            var contactFaker = new Faker<Contact>()
                .RuleFor(c => c.Name, f => nameFaker.Generate())
                .RuleFor(c => c.PetName, f => f.Internet.UserName())
                .RuleFor(c => c.Email, f => f.Internet.Email())
                .RuleFor(c => c.PublicKey, f => f.Lorem.Word())
                .RuleFor(c => c.Picture, f => pictureFaker.Generate());

            var messageDataFaker = new Faker<MessageData>()
                .RuleFor(m => m.Id, f => f.Random.Int())
                .RuleFor(m => m.When, f => f.Date.Past())
                .RuleFor(m => m.Text, f => f.Lorem.Word())
                .RuleFor(m => m.IsRead, f => f.Random.Bool())
                .RuleFor(m => m.IsIncoming, f => f.Random.Bool())
                .RuleFor(m => m.MessageId, f => f.Random.Int());

            var messageFaker = new Faker<Message>()
                .RuleFor(m => m.From, f => contactFaker.Generate())
                .RuleFor(m => m.Id, f => f.Random.Int())
                .RuleFor(m => m.ContactId, f => f.Random.Int())
                .RuleFor(m => m.Messages, f => messageDataFaker.GenerateBetween(1, f.Random.Int(3, 5)))
                .FinishWith((f, m) =>
                {
                    m.ContactId = m.From?.Id ?? 0;
                    foreach (var item in m.Messages)
                    {
                        item.MessageId = m.Id;
                    }
                });

            var newMessages = messageFaker.GenerateBetween(1, faker.Random.Int(2, 4));

            await _db.UpdateMessagesAsync(newMessages.ToObservableCollection());
        }
    }

    public override Task OnAppearing()
    {
        // Load data
        LoadData();
        return Task.CompletedTask;
    }

    private void LoadData()
    {
        //var messages = await _db.GetMessagesAsync();
        var faker = new Faker();
        var nameFaker = new Faker<Name>()
                .RuleFor(n => n.First, f => f.Person.FirstName)
                .RuleFor(n => n.Last, f => f.Person.LastName);

        var pictureFaker = new Faker<Picture>()
            .RuleFor(p => p.Url, f => new(f.Internet.Avatar()));

        var contactFaker = new Faker<Contact>()
            .RuleFor(c => c.Id, f => f.UniqueIndex)
            .RuleFor(c => c.Name, f => nameFaker.Generate())
            .RuleFor(c => c.PetName, f => f.Internet.UserName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.PublicKey, f => f.Lorem.Word())
            .RuleFor(c => c.Picture, f => pictureFaker.Generate());

        var contacts = contactFaker.GenerateBetween(1, faker.Random.Int(2, 5));

        var messages = new List<Message>();
        for (int i = 0; i < faker.Random.Int(2, 20); i++)
        {
            var messageContact = contacts[faker.Random.Int(0, contacts.Count - 1) % contacts.Count];
            var messageId = i;
            messages.Add(new Message()
            {
                ContactId = messageContact.Id,
                Id = messageId,
                From = messageContact,
                Messages = new()
                {
                    new MessageData(){
                        MessageId = messageId,
                        Id = i,
                        IsIncoming = faker.Random.Bool(),
                        IsRead = faker.Random.Bool(),
                        Text = faker.Lorem.Text(),
                        When = faker.Date.Recent()
                    }
                }
            });
        }

        Messages.AddIfNotContains(messages);
    }
}