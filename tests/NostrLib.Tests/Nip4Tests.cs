using System.Linq;
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

        public static bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
        }

        [Fact]
        public async Task Nip04_CanEncrypt()
        {
            // Arrange
            var randomSender = NostrClient.GenerateKey();
            var (ev, receiverKp) = GivenSampleEvent(randomSender.PublicKey);

            // Act
            var origialContent = ev.Content;
            Output.WriteLine("Content: " + ev.Content);
            Output.WriteLine("SenderPub: " + randomSender.PublicKey);
            Output.WriteLine("SenderPriv: " + randomSender.PrivateKey);
            Output.WriteLine("ReceiverPub: " + receiverKp.PublicKey);
            Output.WriteLine("ReceiverPriv: " + receiverKp.PrivateKey);
            await ev.EncryptNip04Event(WithKey(randomSender.PrivateKey));

            // Assert
            ev.ShouldSatisfyAllConditions(
                e => e.Tags.Any(t => t.Data.Contains(receiverKp.PublicKey))
            );

            ev.Content.ShouldSatisfyAllConditions(
                x => x.ShouldNotBeNullOrEmpty(),
                x => x?.ShouldContain("?iv"),
                x => x?.ShouldNotContain(origialContent!),
                x => IsBase64String(x?.Split("?iv=")[0] ?? string.Empty).ShouldBeTrue(),    // encrypted content should be base64
                x => IsBase64String(x?.Split("?iv=")[1] ?? string.Empty).ShouldBeTrue()     // iv should be base64
            );

            Output.WriteLine("EncContent: " + ev.Content);
        }

        private static (NostrEvent<string> ev, NostrKeyPair receiverKp) GivenSampleEvent(string senderPubKey, string? content = null)
        {
            var randomReceiver = NostrClient.GenerateKey();
            var faker = new Faker<NostrEvent<string>>()
                .RuleFor(e => e.Content, f => f.Random.Words(f.Random.Int(1, 3)));

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
            var encContent = "VoBTXkL1nSU/jZ1boXJP2RqAeqwcu9tNqpNyBY3nD3XZyeGjmfpOXXU7ak2usVX9?iv=vmQ0tp50YNVRi8iaoI4B1w==";
            var sampleEvent = GivenSampleEvent("cb697f0ab15d86bb1c930d01af8a15c19a03a1827a05083417ea0ebb1d9e20c2", encContent);
            sampleEvent.ev.Tags.Clear();
            sampleEvent.ev.Tags.Add(new NostrEventTag() { TagIdentifier = "p", Data = new List<string>() { "bc564b9dbf02757c15f7a5a95ed3a0c2e8eb0e239111b1e88396bee51eff1316" } });

            // Act
            await sampleEvent.ev.DecryptNip04Event(WithKey("df2cc9bebc1801f000355f265b23f1828c922377b924a5391f07f20b0f71bcd2"));

            // Assert
            sampleEvent.ev.Content.ShouldSatisfyAllConditions(
                x => x.ShouldNotBeNullOrEmpty(),
                x => x.ShouldBe("quantifying productize")
            );
        }

        [Fact]
        public async Task Nip04_ExceptionOnBadContent()
        {

        }
    }
}
