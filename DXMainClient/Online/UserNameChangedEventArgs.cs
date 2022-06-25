using System;

namespace DTAClient.Online;

public class UserNameChangedEventArgs : EventArgs
{
    public UserNameChangedEventArgs(string oldUserName, IRCUser user)
    {
        OldUserName = oldUserName;
        User = user;
    }

    public string OldUserName { get; }

    public IRCUser User { get; }
}