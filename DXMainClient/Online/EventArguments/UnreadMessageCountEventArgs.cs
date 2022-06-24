using System;

namespace DTAClient.Online.EventArguments;

public class UnreadMessageCountEventArgs : EventArgs
{
    public UnreadMessageCountEventArgs(int unreadMessageCount)
    {
        UnreadMessageCount = unreadMessageCount;
    }


    public int UnreadMessageCount { get; set; }
}