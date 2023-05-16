using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NostrLib.Models;

namespace NuSocial.Messages
{
    public class NostrEventsChangedMessage : ValueChangedMessage<(string subscriptionId, INostrEvent[] events, Uri known)>
    {
        public NostrEventsChangedMessage((string subscriptionId, INostrEvent[] events, Uri known) value) : base(value)
        {
        }
    }
}
