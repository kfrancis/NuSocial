using System.Collections.Concurrent;
using NostrLib;

namespace NuSocial.Services
{
    public interface IRelayService
    {
        bool AddBlockedRelay(Uri uri);

        void ClearBlockedRelays();

        Task<IEnumerable<RelayItem>> FetchCurrentRecommendedRelaysAsync(CancellationToken cancellationToken = default);

        bool IsBlockedRelay(Uri uri);

        bool RemoveBlockedRelay(Uri uri);
    }

    public class RelayService : IRelayService
    {
        private static ConcurrentDictionary<Uri, Uri> s_blockedRelays = new();

        public RelayService()
        {
            var saved = Preferences.Default.Get<Dictionary<Uri, Uri>>("blocked-relays", new Dictionary<Uri, Uri>());
            if (saved != null)
            {
                s_blockedRelays = new ConcurrentDictionary<Uri, Uri>(saved);
            }
        }

        public bool AddBlockedRelay(Uri uri)
        {
            if (!s_blockedRelays.ContainsKey(uri))
            {
                var isSuccess = s_blockedRelays.TryAdd(uri, uri);
                if (isSuccess)
                {
                    Preferences.Default.Set<Dictionary<Uri, Uri>>("blocked-relays", s_blockedRelays.ToDictionary(x => x.Key, x => x.Value));
                }
                return isSuccess;
            }
            else
            {
                return false;
            }
        }

        public void ClearBlockedRelays()
        {
            s_blockedRelays.Clear();
            Preferences.Default.Remove("blocked-relays");
        }

        public Task<IEnumerable<RelayItem>> FetchCurrentRecommendedRelaysAsync(CancellationToken cancellationToken = default)
        {
            // Can we query nostr.watch?
            return Task.FromResult(new List<RelayItem>().AsEnumerable());
        }

        public bool IsBlockedRelay(Uri uri)
        {
            return s_blockedRelays.ContainsKey(uri);
        }

        public bool RemoveBlockedRelay(Uri uri)
        {
            if (s_blockedRelays.ContainsKey(uri))
            {
                var tryRemoveResult = s_blockedRelays.TryRemove(uri, out _);

                if (tryRemoveResult)
                    Preferences.Default.Set<Dictionary<Uri, Uri>>("blocked-relays", s_blockedRelays.ToDictionary(x => x.Key, x => x.Value));

                return tryRemoveResult;
            }
            else
            {
                return false;
            }
        }
    }
}
