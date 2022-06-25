using System;
using System.Collections.Generic;
using System.Linq;
using DTAClient.Domain.Multiplayer;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class GameModeMapFilter
{
    public GameModeMapFilter(Func<List<GameModeMap>> filterAction)
    {
        GetGameModeMaps = filterAction;
    }

    public Func<List<GameModeMap>> GetGameModeMaps { get; set; }

    public bool Any() => GetGameModeMaps().Any();
}