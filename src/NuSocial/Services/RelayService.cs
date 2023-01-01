using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NostrLib;

namespace NuSocial.Services
{
    public class RelayService : IRelayService
    {
        public Task<IEnumerable<RelayItem>> FetchCurrentRecommendedRelaysAsync(CancellationToken cancellationToken = default)
        {
            // Can we query nostr.watch?
            return Task.FromResult(new List<RelayItem>().AsEnumerable());
        }
    }

    public interface IRelayService
    {
        Task<IEnumerable<RelayItem>> FetchCurrentRecommendedRelaysAsync(CancellationToken cancellationToken = default);
    }
}
