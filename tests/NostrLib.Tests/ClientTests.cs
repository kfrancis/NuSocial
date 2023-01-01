using Shouldly;
using Xunit.Abstractions;

namespace NostrLib.Tests
{
    public class ClientTests : ClientBaseFixture
    {
        private const string TestPubKey = "d2a1a81f306d2c096109e593750af2d4a21d510b7518a19f3a7a2f5686fa4689";
        private readonly RelayItem[] _relays;

        public ClientTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _relays = new[] {
                new RelayItem() { Uri = new Uri("wss://nostr-relay.wlvs.space") },
                new RelayItem() { Uri = new Uri("wss://nostr.onsats.org") },
                new RelayItem() { Uri = new Uri("wss://relay.nostr.info") },
                new RelayItem() { Uri = new Uri("wss://nostr-pub.semisol.dev") },
            };
        }

        [Fact]
        public async Task Client_CanConnect()
        {
            // Arrange
            using var client = new NostrClient(TestPubKey, _relays);
            var didCallbackRun = false;
            var cb = (object sender) => { didCallbackRun = true; };
            var cts = new CancellationTokenSource();

            // Act
            await client.ConnectAsync(cb, cancellationToken: cts.Token);

            // Assert
            didCallbackRun.ShouldBeTrue();
            client.IsConnected.ShouldBeTrue();
        }

        [Fact]
        public async Task Client_CanDisconnect()
        {
            // Arrange
            using var client = new NostrClient(TestPubKey, _relays);
            var cts = new CancellationTokenSource();
            await client.ConnectAsync(cancellationToken: cts.Token);

            // Act
            await client.DisconnectAsync(cancellationToken: cts.Token);

            // Assert
            client.IsConnected.ShouldBeFalse();
        }

        [Fact]
        public async Task Client_GetPostsNeedsKey()
        {
            // Arrange
            using var client = await Connect();

            // Act
            try
            {
                var posts = await client.GetPostsAsync();
                Assert.Fail("Should have required a key.");
            }
            catch (InvalidOperationException ex)
            {
                ex.ShouldSatisfyAllConditions(
                    x => x.ShouldNotBeNull(),
                    x => x.Message.ShouldNotBeNullOrWhiteSpace());
            }
        }

        [Fact]
        public async Task Client_GetProfile()
        {
            // Arrange
            using var client = await Connect(TestPubKey);

            // Act
            var profile = await client.GetProfileAsync(TestPubKey);

            // Assert
            profile.ShouldSatisfyAllConditions(
                p => p.Name.ShouldNotBeNullOrEmpty(),
                p => p.Name.ShouldBe("testNuSocialName")
            );
        }

        [Fact]
        public async Task Client_GetGlobalPosts()
        {
            // Arrange
            using var client = await Connect(TestPubKey);

            // Act
            var posts = await client.GetGlobalPostsAsync();

            // Assert
            posts.ShouldSatisfyAllConditions(
                p => p.ShouldNotBeNull(),
                p => p.Any().ShouldBeTrue()
            );
        }

        [Fact]
        public async Task Client_GetPostsWithKey()
        {
            // Arrange
            using var client = await Connect(TestPubKey);

            // Act
            var posts = await client.GetPostsAsync();

            // Assert
            posts.ShouldSatisfyAllConditions(
                p => p.ShouldNotBeNull(),
                p => p.Any().ShouldBeTrue(),
                p => p.Count().ShouldBe(1)
            );
        }

        private async Task<NostrClient> Connect(string key = "")
        {
            var client = new NostrClient(key, _relays);
            var cts = new CancellationTokenSource();
            await client.ConnectAsync(cancellationToken: cts.Token);
            return client;
        }
    }
}
