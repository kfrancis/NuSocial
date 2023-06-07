using Microsoft.Extensions.Localization;
using NuSocial.Localization;
using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Services
{
    public interface IAuthorService
    {
        bool AddBlockedAuthor(string key);

        void ClearBlockedAuthors();

        Task<Profile> GetInfoAsync(string key, CancellationToken cancellationToken = default);

        bool IsBlockedAuthor(string key);

        bool RemoveBlockedAuthor(string key);
    }

    public class AuthorService : IAuthorService, ISingletonDependency
    {
        private const string _blockedKey = "blocked-authors";
        private readonly ConcurrentDictionary<string, string> _blocked = new();
        private readonly ConcurrentDictionary<string, Profile> _cache = new();
        private readonly INostrService _nostr;
        private readonly IStringLocalizer<NuSocialResource> _localizer;

        public AuthorService(INostrService nostrService, IStringLocalizer<NuSocialResource> localizer)
        {
            _nostr = nostrService;
            _localizer = localizer;
            var saved = Preferences.Default.Get<Dictionary<string, string>>(_blockedKey, new Dictionary<string, string>());
            if (saved != null)
            {
                _blocked = new ConcurrentDictionary<string, string>(saved);
            }
        }

        public bool AddBlockedAuthor(string key)
        {
            if (!_blocked.ContainsKey(key))
            {
                var isSuccess = _blocked.TryAdd(key, key);
                if (isSuccess)
                {
                    Preferences.Default.Set(_blockedKey, _blocked.ToDictionary(x => x.Key, x => x.Value));
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
            _blocked.Clear();
            Preferences.Default.Remove(_blockedKey);
        }

        public async Task<Profile> GetInfoAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_blocked.TryGetValue(key, out _))
            {
                return new Profile() { Name = _localizer["Blocked"] };
            }

            if (_cache.TryGetValue(key, out var info))
            {
                return info;
            }
            else
            {
                try
                {
                    var result = await _nostr.GetProfileAsync(key, ct: cancellationToken);
                    _cache.TryAdd(key, result);
                    return result;
                }
                catch (Exception)
                {
                    return new Profile() { Name = key };
                }
            }
        }

        public bool IsBlockedAuthor(string key)
        {
            return _blocked.ContainsKey(key);
        }

        public bool RemoveBlockedAuthor(string key)
        {
            if (_blocked.ContainsKey(key))
            {
                var tryRemoveResult = _blocked.TryRemove(key, out _);

                if (tryRemoveResult)
                    Preferences.Default.Set(_blockedKey, _blocked.ToDictionary(x => x.Key, x => x.Value));

                return tryRemoveResult;
            }
            else
            {
                return false;
            }
        }
    }
}