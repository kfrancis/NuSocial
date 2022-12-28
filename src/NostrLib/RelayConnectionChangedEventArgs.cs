using System;
using NostrLib.Models;

namespace NostrLib
{
    public class PostReceivedEventArgs : EventArgs
    {
        public PostReceivedEventArgs(NostrPost nostrPost)
        {
            NostrPost = nostrPost;
        }

        public NostrPost NostrPost { get; }
    }

    public class RelayPostEventArgs : EventArgs
    {
        public RelayPostEventArgs(string? eventId, bool wasSuccessful, string? message)
        {
            EventId = eventId;
            WasSuccessful = wasSuccessful;
            Message = message;
        }

        public string? EventId { get; }
        public bool WasSuccessful { get; }
        public string? Message { get; }
    }

    public class RelayNoticeEventArgs : EventArgs
    {
        public RelayNoticeEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public class RelayConnectionChangedEventArgs : EventArgs
    {
        public RelayConnectionChangedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; }
    }
}