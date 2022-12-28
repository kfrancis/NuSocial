using NostrLib.Models;
using Shouldly;
using System.Diagnostics;
using Xunit.Abstractions;

namespace NostrLib.Tests
{
    public class ClientTests : ClientBaseFixture
    {
        public ClientTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
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
