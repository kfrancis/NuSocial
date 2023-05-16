using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NNostr.Client;

namespace NuSocial.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly NostrClient _nostrClient;
        private static readonly ConcurrentDictionary<string, NostrProfile> s_cache = new();
        private static ConcurrentDictionary<string, string> s_blocked = new();
        private const string BlockedKey = "blocked-authors";

        public AuthorService(NostrClient nostrClient)
        {
            _nostrClient = nostrClient;

            var saved = Preferences.Default.Get<Dictionary<string, string>>(BlockedKey, new Dictionary<string, string>());
            if (saved != null)
            {
                s_blocked = new ConcurrentDictionary<string, string>(saved);
            }
        }

        public async Task<NostrProfile> GetInfoAsync(string key, CancellationToken cancellationToken = default)
        {
            if (s_blocked.TryGetValue(key, out _))
            {
                return new NostrProfile() { Name = "Blocked" };
            }

            if (s_cache.TryGetValue(key, out var info))
            {
                return info;
            }
            else
            {
                try
                {
                    var result = await _nostrClient.GetProfileAsync(key, cancellationToken);
                    s_cache.TryAdd(key, result);
                    return result;
                }
                catch (Exception)
                {
                    return new NostrProfile() { Name = key };
                }
            }
        }

        public bool AddBlockedAuthor(string key)
        {
            if (!s_blocked.ContainsKey(key))
            {
                var isSuccess = s_blocked.TryAdd(key, key);
                if (isSuccess)
                {
                    Preferences.Default.Set(BlockedKey, s_blocked.ToDictionary(x => x.Key, x => x.Value));
                }
                return isSuccess;
            }
            else
            {
                return false;
            }
        }
        public void ClearBlockedAuthors()
        {
            s_blocked.Clear();
            Preferences.Default.Remove(BlockedKey);
        }

        public bool IsBlockedAuthor(string key)
        {
            return s_blocked.ContainsKey(key);
        }
        public bool RemoveBlockedAuthor(string key)
        {
            if (s_blocked.ContainsKey(key))
            {
                var tryRemoveResult = s_blocked.TryRemove(key, out _);

                if (tryRemoveResult)
                    Preferences.Default.Set(BlockedKey, s_blocked.ToDictionary(x => x.Key, x => x.Value));

                return tryRemoveResult;
            }
            else
            {
                return false;
            }
        }
    }

    public interface IAuthorService
    {
        Task<NostrProfile> GetInfoAsync(string key, CancellationToken cancellationToken = default);
        bool AddBlockedAuthor(string key);
        void ClearBlockedAuthors();
        bool IsBlockedAuthor(string key);
        bool RemoveBlockedAuthor(string key);
    }
}
