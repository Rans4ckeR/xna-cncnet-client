using System;
using System.Collections.Generic;
using System.Linq;
using DTAClient.Domain.Multiplayer;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    internal sealed class GameModeMapFilter
    {
        public Func<List<GameModeMap>> GetGameModeMaps;

        public GameModeMapFilter(Func<List<GameModeMap>> filterAction)
        {
            GetGameModeMaps = filterAction;
        }

        public bool Any() => GetGameModeMaps().Any();
    }
}