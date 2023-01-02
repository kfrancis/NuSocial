using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NostrLib;
using NostrLib.Models;

namespace NuSocial.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly INostrClient _nostrClient;
        private static ConcurrentDictionary<string, NostrProfile> _cache = new();

        public AuthorService(INostrClient nostrClient)
        {
            _nostrClient = nostrClient;
        }

        public async Task<NostrProfile> GetInfoAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var info))
            {
                return info;
            }
            else
            {
                try
                {
                    var result = await _nostrClient.GetProfileAsync(key, cancellationToken);
                    _cache.TryAdd(key, result);
                    return result;
                }
                catch (Exception)
                {
                    return new NostrProfile() { Name = key };
                }
            }
        }
    }

    public interface IAuthorService
    {
        Task<NostrProfile> GetInfoAsync(string key, CancellationToken cancellationToken = default);
    }
}
