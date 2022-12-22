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
        public async void Client_CanConnectAndListen()
        {
            ArgumentNullException.ThrowIfNull(Client);

            var filters = new List<NostrSubscriptionFilter>() {
                new NostrSubscriptionFilter(){  Kinds = new[]{ 1, 7 } }
            };
            var cts = new CancellationTokenSource();

            int postCount = 0;
            try
            {
                Client.PostReceived += (sender, post) => {
                    var rawEvent = post.RawEvent as INostrEvent<string>;
                    Output.WriteLine($"Received post {rawEvent?.Id}: {rawEvent?.Content}");
                    postCount++;
                };
                cts.CancelAfter(TimeSpan.FromSeconds(1));
                await Client.ConnectAsync("id", filters.ToArray(), cts.Token);
            }
            finally
            {
                Client.PostReceived -= (s, post) => { };
            }

            Output.WriteLine($"Received {postCount} posts");
            postCount.ShouldBeGreaterThan(0);
        }
    }
}