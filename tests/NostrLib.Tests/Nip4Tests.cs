using Bogus;
using NBitcoin.Secp256k1;
using NostrLib.Models;
using NostrLib.Nips;
using Shouldly;
using Xunit.Abstractions;

namespace NostrLib.Tests
{
    public class Nip04Tests : ClientBaseFixture
    {
        public Nip04Tests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        public static bool IsBase64String(string base64)
        {
            var buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out var _);
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

        [Theory]
        [InlineData("Fresh primary", "337e9aae24a0e921f78173794c6d037d993b4aa4e0c0df1c8ffb0c6f6caae18d", "1ae4c693d7570dd2cb90e0670e3f79ccafb4cdda52eca21cc9cf5224429b3f5b", "d582c73494e7fe819cc835c19000523f51be9d94fc1a494e6f7be628c6f40e11", "s1b4rM6Bwanxigo0pqe3xeQ5/bM8cUIsD9DpQETRCyk=?iv=1+K6d7IN8S92gi+Rys5g3g==")]
        [InlineData("needs-based interactive", "732ec6322130fb05d8434c7616c99a8e4e40aeb1dce2a7fada6cbd8a9849a41c", "9a359ff4d3814ed83d19990c57f2cd8ca0692a594809cc3f768b23a9cfeb6c7e", "3cdde4bd235d8c84dc24a55710647a505745137e34b9560d6ff7cc287d046853", "8+scz66ksDGbdebpTyLoJlyi1SRAU037S2iMrRFMShIxmANxv6atxHKzQ8kdTObp?iv=o532juHfU8eZaKCWoIsE9A==")]
        [InlineData("1080p", "c921e32db57da6b87396506650432e9720730086566a0f617872dbbb1314f11a", "d6d40605ddb41913878cd87bd8158fa91e9d46348239b862651355dba176cdfd", "13b6b90fe00f2fabd9ff25d48d6a5609f885d25a434cd24e5bcb4e8309211257", "DlzRWhzT4J9NV+NrdXPdbw==?iv=E5mkm0Var1Q1Fit5LBY4Vw==")]
        [InlineData("Checking Account Solutions user-facing", "4ec3337648ecb32b0fe42e2f8e04ccae309cc79ebc2d66321f6bd8c7a4e8f897", "4cf79ea6845c2005be9691cd7da3a6c35b639b0a9de4c484c638077ea483f52c", "009a940e0050ced75c69974da70be37716aa98b1b6c0cdddcb09d2efd27bc9c5", "m6UJXO43TTRjbEH9o6mNVxja6/icwzt+n7VQPvbTwS8g/Z0iCvBP0hHty7NQr2V3U47oEC37mE3RKeh2Jbfa/hJaRwv4k0BJ26V3wfrSDaA=?iv=M5ZwXwH1i6KoxSCgaGlluA==")]
        [InlineData("Station", "5bcba00515d9f8bc426a66f152a65bfec0ff57990c6857a0c2687a480ad315de", "649eb92994601f4e0393a3420307e0ffe3f716f1c4599e938decc7c2ca25c165", "3e608426ac2f0e7615b0768376c05faf05b2140001e996530389e85ad6df26df", "l06C22AvjgYpR8AdRvekbg==?iv=Ola1RB/sjAN/aewGOUs6dA==")]
        public async Task Nip04_CanDecrypt(string content, string senderPub, string receiverPub, string receiverPriv, string encContent)
        {
            // Arrange
            var (ev, receiverKp) = GivenSampleEvent(senderPub, encContent);
            ev.Tags.Clear();
            ev.Tags.Add(new NostrEventTag() { TagIdentifier = "p", Data = new List<string>() { receiverPub } });

            // Act
            await ev.DecryptNip04Event(WithKey(receiverPriv));

            // Assert
            ev.Content.ShouldSatisfyAllConditions(
                x => x.ShouldNotBeNullOrEmpty(),
                x => x.ShouldBe(content)
            );
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
            Output.WriteLine("ReceiverPub: " + receiverKp.PublicKey);
            Output.WriteLine("ReceiverPriv: " + receiverKp.PrivateKey);
            await ev.EncryptNip04Event(WithKey(randomSender.PrivateKey));

            // Assert
            ev.ShouldSatisfyAllConditions(
                e => e.Tags.Any(t => t.Data.Contains(receiverKp.PublicKey)).ShouldBeTrue(),
                e => e.PublicKey.ShouldBe(randomSender.PublicKey)
            );

            ev.Content.ShouldSatisfyAllConditions(
                x => x.ShouldNotBeNullOrEmpty(),
                x => x?.ShouldContain("?iv"),
                x => x?.ShouldNotContain(origialContent!),
                x => IsBase64String(x?.Split("?iv=")[0] ?? string.Empty).ShouldBeTrue(),    // encrypted content should be base64
                x => IsBase64String(x?.Split("?iv=")[1] ?? string.Empty).ShouldBeTrue()     // iv should be base64
            );

            Output.WriteLine("EncContent: " + ev.Content);

            Output.WriteLine($"[InlineData(\"{origialContent}\",\"{randomSender.PublicKey}\",\"{receiverKp.PublicKey}\",\"{receiverKp.PrivateKey}\",\"{ev.Content}\")]");
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
                e.Content = content; // overwrite if there's content passed in
            }
            e.PublicKey = senderPubKey;
            e.Tags ??= new();
            e.Tags.Add(new NostrEventTag() { TagIdentifier = "p", Data = new List<string>() { randomReceiver.PublicKey } });
            return (e, randomReceiver);
        }

        /// <summary>
        /// Provide a random private key or a formatted private key from string param
        /// </summary>
        /// <param name="privateKey">The hex private key</param>
        /// <returns>The ECPrivKey</returns>
        private static ECPrivKey WithKey(string? privateKey = null)
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
    }
}
