using System;

namespace DTAClient.Online;

public class UserNameEventArgs : EventArgs
{
    public UserNameEventArgs(string userName)
    {
        UserName = userName;
    }

    public string UserName { get; private set; }
}