using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Messages
{
    public class LogoutMessage : ValueChangedMessage<bool?>
    {
        public LogoutMessage(bool? value = null) : base(value)
        {
        }
    }

    public class DataLoadedMessage : ValueChangedMessage<bool?>
    {
        public DataLoadedMessage(bool? value = null) : base(value)
        {
        }
    }
}

