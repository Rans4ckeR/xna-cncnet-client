using System;

namespace DTAClient.Online.EventArguments;

public class JoinUserEventArgs : EventArgs
{
    public JoinUserEventArgs(IRCUser ircUser)
    {
        IrcUser = ircUser;
    }

    public IRCUser IrcUser { get; }
}