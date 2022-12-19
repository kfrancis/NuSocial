using Bogus;
using Contact = NuSocial.Models.Contact;

namespace NuSocial.Services;

public class SampleDataService
{
    public async Task<IEnumerable<Post>> GetItems()
    {
        var contactFaker = new Faker<Contact>()
            .RuleFor(c => c.Name, f => new Name(f.Person.FirstName, f.Person.LastName))
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.Name.First, c.Name.Last))
            .RuleFor(c => c.Picture, f => new Picture(new Uri(f.Person.Avatar)));

        var postFaker = new Faker<Post>()
            .RuleFor(p => p.Contact, f => contactFaker.Generate())
            .RuleFor(p => p.Content, f => f.Lorem.Word());

        return postFaker.GenerateBetween(5, 20);
    }
}
