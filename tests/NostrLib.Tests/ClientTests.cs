using NostrLib.Models;
using Shouldly;
using System.Diagnostics;
using System.Text.Json;
using Xunit.Abstractions;

namespace NostrLib.Tests
{
    public class ClientTests : ClientBaseFixture
    {
        public ClientTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void CanDeserialize()
        {
            var json = "{\r\n    \"id\": \"5d3a5ac23169eefede06bd59fcdf3713b6c86b73e330848a8cfc7bf03174d591\",\r\n    \"pubkey\": \"1ad34e8aa265df5bd6106b4535a6a82528141efd800beb35b6413d7a8298741f\",\r\n    \"created_at\": 1671997551,\r\n    \"kind\": 1,\r\n    \"tags\": [[\"p\", \"5e7ae588d7d11eac4c25906e6da807e68c6498f49a38e4692be5a089616ceb18\"]],\r\n    \"content\": \"#[0] Verifying My Public Key: \\\"djbyter\\\"\",\r\n    \"sig\": \"eb7fdddfdd6118bf0e21b5c467cd0b78aa1fffe5bd23cd30662971e053828a12161d92f9d2d1411eab1f5a834a37dbd896991f59f246a507f60917c496b0e37e\"\r\n}\r\n";

            var ev = JsonSerializer.Deserialize<NostrEvent<string>>(json);

            ev.ShouldNotBeNull();
            ev.Tags.Count.ShouldBe(1);
        }

        [Fact]
        public void CanDeserializeSame()
        {
            try
            {
                var jsonTxt = "{\r\n    \"id\": \"5d3a5ac23169eefede06bd59fcdf3713b6c86b73e330848a8cfc7bf03174d591\",\r\n    \"pubkey\": \"1ad34e8aa265df5bd6106b4535a6a82528141efd800beb35b6413d7a8298741f\",\r\n    \"created_at\": 1671997551,\r\n    \"kind\": 1,\r\n    \"tags\": [[\"p\", \"5e7ae588d7d11eac4c25906e6da807e68c6498f49a38e4692be5a089616ceb18\"]],\r\n    \"content\": \"#[0] Verifying My Public Key: \\\"djbyter\\\"\",\r\n    \"sig\": \"eb7fdddfdd6118bf0e21b5c467cd0b78aa1fffe5bd23cd30662971e053828a12161d92f9d2d1411eab1f5a834a37dbd896991f59f246a507f60917c496b0e37e\"\r\n}\r\n";
                using var doc = JsonDocument.Parse(jsonTxt);
                var json = doc.RootElement;
                var ev = JsonSerializer.Deserialize<NostrEvent<string>>(jsonTxt);
                ev.ShouldNotBeNull();
                ev.Tags.Count.ShouldBe(1);
            }
            catch (InvalidOperationException ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [Fact]
        public async void Client_CanDisconnect()
        {
            ArgumentNullException.ThrowIfNull(Client);

            var cts = new CancellationTokenSource();
            await Client.ConnectAsync(cancellationToken: cts.Token);
            await Client.DisconnectAsync(cancellationToken: cts.Token);
        }

        [Fact]
        public async void Client_CanConnectAndListen()
        {
            ArgumentNullException.ThrowIfNull(Client);
            var filter = new NostrSubscriptionFilter();
            filter.Kinds.Add(NostrKind.SetMetadata);
            filter.Kinds.Add(NostrKind.Reaction);
            var filters = new List<NostrSubscriptionFilter>() { filter };
            var cts = new CancellationTokenSource();
            var callback = (NostrClient sender) =>
            {
                sender.PostReceived -= (s, post) => { };
            };

            int postCount = 0;
            Client.PostReceived += (sender, post) =>
            {
                var rawEvent = post.RawEvent as INostrEvent<string>;
                Output.WriteLine($"Received post {rawEvent?.Id}: {rawEvent?.Content}");
                postCount++;
            };
            cts.CancelAfter(TimeSpan.FromMilliseconds(500));
            await Client.ConnectAsync(callback, cts.Token);

            Output.WriteLine($"Received {postCount} posts");
            postCount.ShouldBeGreaterThan(0);
        }
    }
}
