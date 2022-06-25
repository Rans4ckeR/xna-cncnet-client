using System;

namespace DTAClient.Online;

public class UserNameIndexEventArgs : EventArgs
{
    public UserNameIndexEventArgs(int index, string userName)
    {
        UserIndex = index;
        UserName = userName;
    }

    public int UserIndex { get; private set; }

    public string UserName { get; private set; }
}