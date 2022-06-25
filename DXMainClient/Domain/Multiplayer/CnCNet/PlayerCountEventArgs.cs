using System;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal class PlayerCountEventArgs : EventArgs
{
    public PlayerCountEventArgs(int playerCount)
    {
        PlayerCount = playerCount;
    }

    public int PlayerCount { get; set; }
}