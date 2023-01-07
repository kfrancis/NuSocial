using System.Runtime.CompilerServices;
using Bogus;
using NBitcoin;
using NBitcoin.Secp256k1;
using NostrLib.Models;
using NostrLib.Nips;
using Shouldly;
using Xunit.Abstractions;

namespace NostrLib.Tests
{
    public class Nip4Tests : ClientBaseFixture
    {
        private const string TestPubKey = "d2a1a81f306d2c096109e593750af2d4a21d510b7518a19f3a7a2f5686fa4689";
        private readonly RelayItem[] _relays;

        public Nip4Tests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _relays = new[] {
                new RelayItem() { Uri = new Uri("wss://nostr-relay.wlvs.space") },
                //new RelayItem() { Uri = new Uri("wss://nostr.onsats.org") },
                //new RelayItem() { Uri = new Uri("wss://relay.nostr.info") },
                //new RelayItem() { Uri = new Uri("wss://nostr-pub.semisol.dev") },
            };
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        [Fact]
        public void CanGenerateEncryptableEvent()
        {
            var randomSender = NostrClient.GenerateKey();

            var (ev, receiverKp) = GivenSampleEvent(randomSender.PublicKey);

            receiverKp.ShouldSatisfyAllConditions(
                kp => kp.PrivateKey.ShouldNotBeNullOrEmpty(),
                kp => kp.PublicKey.ShouldNotBeNullOrEmpty()
                );

            ev.ShouldSatisfyAllConditions(
                x => x.ShouldNotBeNull(),
                x => x.Tags.ShouldNotBeNull(),
                x => x.Tags.Count.ShouldBe(1),
                x => x.Tags.Any(t => t.Data.Contains(receiverKp.PublicKey)).ShouldBeTrue(),
                x => x.PublicKey.ShouldNotBeNullOrEmpty(),
                x => x.Content.ShouldNotBeNullOrEmpty(),
                x => x.Kind.ShouldBe(NostrKind.EncryptedDM),
                x => x.PublicKey.ShouldBe(randomSender.PublicKey)
            );
        }

        [Fact]
        public async Task Nip04_CanEncrypt()
        {
            // Arrange
            var randomSender = NostrClient.GenerateKey();
            var (ev, receiverKp) = GivenSampleEvent(randomSender.PublicKey);

            // Act
            Output.WriteLine("Content: " + ev.Content);
            Output.WriteLine("SenderPub: " + randomSender.PublicKey);
            Output.WriteLine("SenderPriv: " + randomSender.PrivateKey);
            Output.WriteLine("ReceiverPub: " + receiverKp.PublicKey);
            await ev.EncryptNip04Event(WithKey(randomSender.PrivateKey));

            // Assert
            ev.Content.ShouldSatisfyAllConditions(
                x => x.ShouldNotBeNullOrEmpty(),
                x => x?.ShouldContain("?iv")
            );

            Output.WriteLine("EncContent: " + ev.Content);
        }

        private (NostrEvent<string> ev, NostrKeyPair receiverKp) GivenSampleEvent(string senderPubKey, string? content = null)
        {
            var randomReceiver = NostrClient.GenerateKey();
            var faker = new Faker<NostrEvent<string>>()
                .RuleFor(e => e.Content, f => f.Random.Words(f.Random.Int(1, 20)));

            var e = faker.Generate(1)[0];
            e.Kind = NostrKind.EncryptedDM;
            if (!string.IsNullOrEmpty(content))
            {
                e.Content = content; // overwrite
            }
            e.PublicKey = senderPubKey;
            e.Tags ??= new();
            e.Tags.Add(new NostrEventTag() { TagIdentifier = "p", Data = new List<string>() { randomReceiver.PublicKey } });
            return (e, randomReceiver);
        }

        private ECPrivKey WithKey(string? privateKey = null)
        {
            if (!string.IsNullOrEmpty(privateKey))
            {
                return Context.Instance.CreateECPrivKey(StringToByteArray(privateKey));
            }
            else
            {
                var keys = NostrClient.GenerateKey();
                return Context.Instance.CreateECPrivKey(StringToByteArray(keys.PrivateKey));
            }
        }

        [Fact]
        public async Task Nip04_CanDecrypt()
        {
            // Arrange
            var encContent = "h4pXaZs8AWMTSAuOWmBpyg==?iv=rSZ4Z4ObT40+FO8htVOlng==";
            var sampleEvent = GivenSampleEvent("57d6b1e417d0466e1b4bd8b8bfc08fb9f3b9843155c6c56065d359f5a2e4ffef", encContent);
            sampleEvent.ev.Tags.Clear();
            sampleEvent.ev.Tags.Add(new NostrEventTag() { TagIdentifier = "p", Data = new List<string>() { "0f6cbdcecd15cd7a8061cdc562bd3ae00594a35c66db59dc6614958ccb07b0a9" } });

            // Act
            await sampleEvent.ev.DecryptNip04Event(WithKey("bdd904e4e6ec9d45dd98e6da43e287911642daa9d97b4611c3ecbccdbc02093e"));

            // Assert
            sampleEvent.ev.Content.ShouldSatisfyAllConditions(
                x => x.ShouldNotBeNullOrEmpty(),
                x => x.ShouldBe("Highway")
            );
        }

        [Fact]
        public async Task Nip04_ExceptionOnBadContent()
        {

        }
    }
}
