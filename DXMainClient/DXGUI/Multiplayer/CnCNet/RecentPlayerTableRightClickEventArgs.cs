using System;
using DTAClient.Online;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

public class RecentPlayerTableRightClickEventArgs : EventArgs
{
    public RecentPlayerTableRightClickEventArgs(IRCUser ircUser)
    {
        IrcUser = ircUser;
    }


    public IRCUser IrcUser { get; set; }
}