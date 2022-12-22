using System;
using System.Collections.Generic;
using System.Text;

namespace NostrLib.Models
{
    public class NostrPost
    {
        public NostrPost(INostrEvent ev)
        {
            RawEvent = ev;
        }

        public INostrEvent RawEvent { get; set; }
    }
    
}
