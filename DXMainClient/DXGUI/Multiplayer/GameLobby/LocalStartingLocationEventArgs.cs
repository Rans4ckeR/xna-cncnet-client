using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class LocalStartingLocationEventArgs : EventArgs
{
    public LocalStartingLocationEventArgs(int startingLocationIndex)
    {
        StartingLocationIndex = startingLocationIndex;
    }

    public int StartingLocationIndex { get; set; }
}