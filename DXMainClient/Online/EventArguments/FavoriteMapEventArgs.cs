using System;
using DTAClient.Domain.Multiplayer;

namespace DTAClient.Online.EventArguments;

public class FavoriteMapEventArgs : EventArgs
{
    public FavoriteMapEventArgs(Map map)
    {
        Map = map;
    }

    public Map Map { get; }
}