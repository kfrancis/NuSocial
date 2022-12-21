using NostrLib.Models;
using Shouldly;

namespace NostrLib.Tests
{
    public class ClientTests : ClientBaseFixture
    {
        [Fact]
        public async void Client_CanConnectAndWaitUntilConnected()
        {
            ArgumentNullException.ThrowIfNull(Client);

            var filters = new List<NostrSubscriptionFilter>() {
                new NostrSubscriptionFilter(){  Kinds = new[]{ 1, 7 } }
            };

            var cts = new CancellationTokenSource();
            try
            {
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                Client.Connect("subid", filters.ToArray(), cts.Token);
            }
            catch (TaskCanceledException)
            {

                
            }
            
        }

        [Fact]
        public async void Client_CanSubscribe()
        {
            ArgumentNullException.ThrowIfNull(Client);
            var cts = new CancellationTokenSource();

            var filters = new List<NostrSubscriptionFilter>() {
                new NostrSubscriptionFilter(){  Kinds = new[]{ 1, 7 } }
            };

            //int messageCount = 0;
            //try
            //{
            //    Client.Subscriptions.Add("<sub here>", filters.ToArray());
            //    Client.MessageReceived += (s, msg) =>
            //    {
            //        messageCount++;
            //    };
            //    cts.CancelAfter(TimeSpan.FromSeconds(5));
            //    await Client.Connect(cts.Token);
            //}
            //catch (TaskCanceledException)
            //{
            //}
            //finally
            //{
            //    Client.MessageReceived -= (s, msg) => { };
            //}

            //messageCount.ShouldBeGreaterThan(0);
        }


        [Fact]
        public async void Client_CanCloseSubscription()
        {
            ArgumentNullException.ThrowIfNull(Client);

            //await Client.CloseSubscription("123");
        }
    }
}