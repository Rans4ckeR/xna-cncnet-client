using System;

namespace DTAClient.Online.EventArguments;

public class MultiplayerNameRightClickedEventArgs : EventArgs
{
    public MultiplayerNameRightClickedEventArgs(string playerName)
    {
        PlayerName = playerName;
    }

    public string PlayerName { get; }
}