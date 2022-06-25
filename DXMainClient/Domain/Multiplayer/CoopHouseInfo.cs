namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// Holds information about enemy houses in a co-op map.
/// </summary>
public struct CoopHouseInfo
{
    public CoopHouseInfo(int side, int color, int startingLocation)
    {
        Side = side;
        Color = color;
        StartingLocation = startingLocation;
    }

    /// <summary>
    /// Gets or sets the index of the enemy house's color.
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Gets or sets the index of the enemy house's side.
    /// </summary>
    public int Side { get; set; }

    /// <summary>
    /// Gets or sets the starting location waypoint of the enemy house.
    /// </summary>
    public int StartingLocation { get; set; }
}