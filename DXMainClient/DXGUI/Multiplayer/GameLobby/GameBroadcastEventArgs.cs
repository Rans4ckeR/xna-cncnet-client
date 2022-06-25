using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class GameBroadcastEventArgs : EventArgs
{
    public GameBroadcastEventArgs(string message)
    {
        Message = message;
    }

    public string Message { get; private set; }
}