using Bogus;
using Volo.Abp.DependencyInjection;
using Contact = NuSocial.Models.Contact;

namespace NuSocial.ViewModels;

public partial class MessagesViewModel : BaseViewModel, ITransientDependency
{

    [ObservableProperty]
    private ObservableCollection<Message> _messages = new ObservableCollection<Message>();

    private readonly IDatabase _db;

    public MessagesViewModel(IDialogService dialogService,
                             INavigationService navigationService,
                             IDatabase db)
        : base(dialogService, navigationService)
    {
        _db = db;
    }

    public override async Task InitializeAsync()
    {
        Title = L["Messages"];

        // Load data
        var messages = await _db.GetMessagesAsync();

        if (messages == null || !messages.Any())
        {
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
                .RuleFor(m => m.Messages, f => messageDataFaker.GenerateBetween(1, f.Random.Int(3,5)))
                .FinishWith((f, m) => {
                    m.ContactId = m.From?.Id ?? 0;
                    foreach (var item in m.Messages)
                    {
                        item.Message = m;
                        item.MessageId = m.Id;
                    }
                });

            var message = messageFaker.Generate();

            Messages.AddIfNotContains(message);
            await _db.UpdateMessagesAsync(Messages);
        }
        else
        {
            Messages.AddIfNotContains(messages);
        }

    }
}