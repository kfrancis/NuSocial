using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NostrLib.Models;

namespace NostrLib
{
    public interface INostrClient
    {
        string? PrivateKey { get; set; }
        string? PublicKey { get; set; }
        TimeSpan ReconnectDelay { get; set; }

        Task ConnectAsync(Action<Client>? cb = null, CancellationToken token = default);
        Task DisconnectAsync();
        void Dispose();
        Task<IEnumerable<string>> GetFollowerInfoAsync(string publicKey);
        Task<INostrEvent?> GetFollowingInfoAsync(string publicKey);
        Task<IEnumerable<NostrPost>> GetGlobalPostsAsync(int? limit = null, DateTime? since = null, List<string>? authors = null);
        Task<IEnumerable<NostrPost>> GetPostsAsync();
        Task GetProfileAsync(string publicKey);
        Task SetRelaysAsync(RelayItem[] relayItems);
    }
}