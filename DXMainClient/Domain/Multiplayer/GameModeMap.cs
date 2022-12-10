namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// An instance of a Map in a given GameMode
    /// </summary>
    internal sealed class GameModeMap
    {
        public GameMode GameMode { get; }
        public Map Map { get; }
        public bool IsFavorite { get; set; }

        public GameModeMap(GameMode gameMode, Map map, bool isFavorite)
        {
            GameMode = gameMode;
            Map = map;
            IsFavorite = isFavorite;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (GameMode != null ? GameMode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Map != null ? Map.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsFavorite.GetHashCode();
                return hashCode;
            }
        }
    }
}