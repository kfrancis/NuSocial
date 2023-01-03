using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.Secp256k1;
using NostrLib.Models;

namespace NostrLib
{
    public interface INostrClient
    {
        ECPrivKey? PrivateKey { get; set; }
        string? PublicKey { get; set; }
        TimeSpan ReconnectDelay { get; set; }

        Task ConnectAsync(Action<NostrClient>? cb = null, CancellationToken cancellationToken = default);

        Task DisconnectAsync(CancellationToken cancellationToken = default);

        void Dispose();

        Task<IEnumerable<string>> GetFollowerInfoAsync(string publicKey, CancellationToken cancellationToken = default);

        Task<INostrEvent?> GetFollowingInfoAsync(string publicKey, CancellationToken cancellationToken = default);

        Task<IEnumerable<NostrPost>> GetGlobalPostsAsync(int? limit = null, DateTime? since = null, Collection<string>? authors = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<NostrPost>> GetPostsAsync(CancellationToken cancellationToken = default);

        Task<NostrProfile> GetProfileAsync(string publicKey, CancellationToken cancellationToken = default);

        Task<INostrEvent> SendReplyPostAsync(string message, INostrEvent e);

        Task<INostrEvent> SendTextPostAsync(string message);

        Task SetRelaysAsync(RelayItem[] relayItems, bool shouldConnect = false, CancellationToken cancellationToken = default);

        void UpdateKey(string key, bool isPrivateKey = false);
    }
}
