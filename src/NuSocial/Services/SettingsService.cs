using CommunityToolkit.Mvvm.DependencyInjection;
using NostrLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IDatabase _db;
        private string _key = string.Empty;

        public SettingsService(IDatabase db)
        {
            _db = db;
        }

        public SettingsService()
            : this(Ioc.Default.GetRequiredService<IDatabase>())
        {
        }

        public int? GetLimit()
        {
            return 100;
        }

        public RelayItem[] GetRelays()
        {
            var relays = new List<RelayItem>()
        {
            new RelayItem() { Name = "nostr.ethtozero.fr", Uri = new Uri("wss://nostr.ethtozero.fr") },
            //new RelayItem() { Name = "relay.damus.io", Uri = new Uri("wss://relay.damus.io") },
            //new RelayItem() { Name = "nostr.pwnshop.cloud", Uri = new Uri("wss://nostr.pwnshop.cloud") },
        };
            return relays.ToArray();
        }

        public string GetId()
        {
            return _key;
        }

        public async Task<bool> LoginAsync(string key)
        {
            try
            {
                _key = key;

                var nostr = Ioc.Default.GetService<INostrClient>();
                nostr?.UpdateKey(_key);

                await SecureStorage.Default.SetAsync("key", key);
                //await _db.UpdateUserAsync(new User() { Key = key });

                return true;
            }
            catch (Exception)
            {
                // TODO: Log
            }
            return false;
        }
    }

    public interface ISettingsService
    {
        int? GetLimit();
        RelayItem[] GetRelays();
        Task<bool> LoginAsync(string key);
    }
}
