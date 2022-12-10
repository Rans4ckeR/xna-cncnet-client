using System;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    internal sealed class MapEventArgs : EventArgs
    {
        public MapEventArgs(Map map)
        {
            Map = map;
        }

        public Map Map { get; }
    }
}