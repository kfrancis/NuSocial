using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;
using NNostr.Client;

namespace NuSocial.Messages
{
    public class NostrEventsChangedMessage : ValueChangedMessage<(string subscriptionId, NostrEvent[] events, Uri known)>
    {
        public NostrEventsChangedMessage((string subscriptionId, NostrEvent[] events, Uri known) value) : base(value)
        {
        }
    }
}
