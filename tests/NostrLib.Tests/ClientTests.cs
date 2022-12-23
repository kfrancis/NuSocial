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
            await Client.ConnectAsync("id", Array.Empty<NostrSubscriptionFilter>(),token: cts.Token);

            await Client.DisconnectAsync();
        }

        [Fact]
        public async void Client_CanConnectAndListen()
        {
            ArgumentNullException.ThrowIfNull(Client);

            var filters = new List<NostrSubscriptionFilter>() {
                new NostrSubscriptionFilter(){  Kinds = new[]{ 1, 7 } }
            };
            var cts = new CancellationTokenSource();
            var callback = (Client sender) =>
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
            await Client.ConnectAsync("id", filters.ToArray(), callback, cts.Token);

            Output.WriteLine($"Received {postCount} posts");
            postCount.ShouldBeGreaterThan(0);
        }
    }
}