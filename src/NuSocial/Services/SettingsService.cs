using CommunityToolkit.Mvvm.DependencyInjection;
using NostrLib;

namespace NuSocial.Services
{
    public interface ISettingsService
    {
        string GetId();
        int? GetLimit();

        RelayItem[] GetRelays();

        Task<bool> LoginAsync(string key, bool isPrivate);
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

        /// <summary>
        /// Login by updating the Nostr client key and setting the SecureStorage key for easy re-login
        /// </summary>
        /// <param name="key">The hex key</param>
        /// <param name="isPrivate">Is the key being passed a private key? If so, true. If not (if it's a public key), then false.</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> LoginAsync(string key, bool isPrivate)
        {
            try
            {
                _key = key;

                var nostr = Ioc.Default.GetService<INostrClient>();
                nostr?.UpdateKey(key, isPrivate);

                await SecureStorage.Default.SetAsync("key", key);
                await SecureStorage.Default.SetAsync("isPrivate", isPrivate ? "true" : "false");
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
