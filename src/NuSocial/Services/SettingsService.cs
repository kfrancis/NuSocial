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
        public int? GetLimit()
        {
            return 100;
        }

        public RelayItem[] GetRelays()
        {
            var relays = new List<RelayItem>()
        {
            new RelayItem() { Name = "nostr.ethtozero.fr", Uri = new Uri("wss://nostr.ethtozero.fr") },
            new RelayItem() { Name = "relay.damus.io", Uri = new Uri("wss://relay.damus.io") },
            new RelayItem() { Name = "mule.platanito.org", Uri = new Uri("wss://mule.platanito.org") },
        };
            return relays.ToArray();
        }

        public string GetId()
        {
            return "1ad34e8aa265df5bd6106b4535a6a82528141efd800beb35b6413d7a8298741f";
        }
    }

    public interface ISettingsService
    {
        int? GetLimit();
        RelayItem[] GetRelays();
    }
}
