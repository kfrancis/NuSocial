using CommunityToolkit.Mvvm.Messaging;
using Nostr.Client.Keys;
using NuSocial.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial
{
    public class GlobalSetting
    {
        private User? _user;

        public static GlobalSetting Instance { get; } = new GlobalSetting();

        public User CurrentUser
        {
            get => _user;
            set
            {
                if (value?.PublicKey != null)
                {
                    _user = value;
                    UpdateUser(_user.PublicKey, _user.PrivateKey);
                }
            }
        }

        public bool DemoMode { get; set; }

        private static void UpdateUser(NostrPublicKey publicKey, NostrPrivateKey? privateKey)
        {
            if (privateKey != null)
            {
                WeakReferenceMessenger.Default.Send<NostrUserChangedMessage>(new((publicKey.Hex, privateKey.Hex)));
            }
            else
            {
                WeakReferenceMessenger.Default.Send<NostrUserChangedMessage>(new((publicKey.Hex, string.Empty)));
            }
        }
    }
}
