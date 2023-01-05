using CommunityToolkit.Mvvm.DependencyInjection;
using NostrLib;

namespace NuSocial.Services
{
    public interface ISettingsService
    {
        string GetId();
        int? GetLimit();

        RelayItem[] GetRelays();

        Task<bool> LoginAsync(string key);
    }

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

        public string GetId()
        {
            return _key;
        }

        public int? GetLimit()
        {
            return 100;
        }

        public RelayItem[] GetRelays()
        {
            var relays = new List<RelayItem>()
            {
                //new RelayItem("relay.damus.io"),
                new RelayItem("nostr-relay.wlvs.space"),
                //new RelayItem("nostr-fmt.wiz.biz"),
                //new RelayItem("relay.nostr.bg"),
                //new RelayItem("nostr-oxtr.dev"),
                //new RelayItem("nostr-v0l.io"),
                //new RelayItem("brb.io"),
            };
            return relays.ToArray();
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
}
