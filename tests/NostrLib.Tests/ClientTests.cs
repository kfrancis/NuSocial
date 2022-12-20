using NostrLib.Models;

namespace NostrLib.Tests
{
    public class ClientTests : ClientBaseFixture
    {
        [Fact]
        public async void Client_MessageReceivedAfterConnecting()
        {
            ArgumentNullException.ThrowIfNull(Client);

            Assert.Raises<string>(handler => Client.MessageReceived += handler, handler => Client.MessageReceived -= handler, async () => {
                await Client.Connect();
            });
        }

        [Fact]
        public async void Client_CanConnectAndWaitUntilConnected()
        {
            ArgumentNullException.ThrowIfNull(Client);

            await Client.ConnectAndWaitUntilConnected();
        }

        [Fact]
        public async void Client_CanSubscribe()
        {
            ArgumentNullException.ThrowIfNull(Client);

            await Client.CreateSubscription("123", Array.Empty<NostrSubscriptionFilter>());
        }

        [Fact]
        public async void Client_CanCloseSubscription()
        {
            ArgumentNullException.ThrowIfNull(Client);


            await Client.CloseSubscription("123");
        }
    }
}