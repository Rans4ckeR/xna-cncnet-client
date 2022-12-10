using System.Collections.Generic;
using System.Linq;
using ClientCore;

namespace DTAClient.Domain.Multiplayer
{
    internal sealed class GameModeMapCollection : List<GameModeMap>
    {
        public GameModeMapCollection(IEnumerable<GameMode> gameModes, UserINISettings userIniSettings) :
            base(gameModes.SelectMany(gm => gm.Maps.Select(map =>
                new GameModeMap(gm, map, userIniSettings.IsFavoriteMap(map.Name, gm.Name)))).Distinct())
        {
        }

        public List<GameMode> GameModes => this.Select(gmm => gmm.GameMode).Distinct().ToList();
    }
}