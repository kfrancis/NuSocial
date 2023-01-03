using System.Runtime.CompilerServices;
using NBitcoin;
using NBitcoin.Secp256k1;
using NostrLib.Models;
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
            using var client = new NostrClient(TestPubKey, relays: _relays);
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
            using var client = new NostrClient(TestPubKey, false, _relays);
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
            using var client = new NostrClient("", false, _relays);
            var cts = new CancellationTokenSource();
            await client.ConnectAsync(cancellationToken: cts.Token);

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
        public void CanHandlePrivatePublicKeyFormats()
        {
            var privKeyHex = "7f4c11a9742721d66e40e321ca50b682c27f7422190c14a187525e69e604836a";
            Assert.True(Context.Instance.TryCreateECPrivKey(privKeyHex.DecodHexData(), out var privKey));

            var pubKey = privKey.CreateXOnlyPubKey();
            Assert.Equal("7cef86754ddf07395c289c30fe31219de938c6d707d6b478a8682fc75795e8b9", pubKey.ToBytes().ToHex());
        }

        [Fact]
        public async Task Client_CanPost()
        {
            // Arrange
            using var client = await Connect();
            var message = Guid.NewGuid().ToString();

            // Act
            var result = await client.SendTextPostAsync(message);

            // Assert
            result.ShouldSatisfyAllConditions(
                x => x.ShouldNotBeNull(),
                x => (x is INostrEvent<string>).ShouldBeTrue(),
                x => (x as INostrEvent<string>)!.Verify().ShouldBeTrue(),
                x => (x as INostrEvent<string>)!.Content.ShouldBe(message),
                x => x.Id.ShouldNotBeNullOrEmpty(),
                x => x.Signature.ShouldNotBeNullOrEmpty(),
                x => x.Kind.ShouldBe(NostrKind.TextNote)
            );
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
            var postFetchCount = 10;

            // Act
            var posts = await client.GetGlobalPostsAsync(postFetchCount);

            // Assert
            posts.ShouldSatisfyAllConditions(
                p => p.ShouldNotBeNull(),
                p => p.Any().ShouldBeTrue(),
                p => p.Count().ShouldBeLessThanOrEqualTo(postFetchCount)
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
                p => p.Count().ShouldBeGreaterThanOrEqualTo(1)
            );
        }

        private async Task<NostrClient> Connect(string key = "")
        {
            bool isPrivateKey = false;
            if (string.IsNullOrEmpty(key))
            {
                var keys = NostrClient.GenerateKey();
                key = keys.PrivateKey;

                Output.WriteLine($"Public: {keys.PublicKey}");
                Output.WriteLine($"Private: {keys.PrivateKey}");
                isPrivateKey = true;
            }
            var client = new NostrClient(key, isPrivateKey, _relays);
            var cts = new CancellationTokenSource();
            await client.ConnectAsync(cancellationToken: cts.Token);
            return client;
        }
    }
}
