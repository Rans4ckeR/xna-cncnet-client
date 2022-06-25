using System;

namespace DTAClient.Online;

public class ChannelUserEventArgs : EventArgs
{
    public ChannelUserEventArgs(ChannelUser user)
    {
        User = user;
    }

    public ChannelUser User { get; private set; }
}