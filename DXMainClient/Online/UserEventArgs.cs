using System;

namespace DTAClient.Online;

public class UserEventArgs : EventArgs
{
    public UserEventArgs(IRCUser ircUser)
    {
        User = ircUser;
    }

    public IRCUser User { get; private set; }
}