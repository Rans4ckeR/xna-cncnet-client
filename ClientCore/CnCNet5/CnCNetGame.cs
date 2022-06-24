using Microsoft.Xna.Framework.Graphics;

namespace ClientCore.CnCNet5;

/// <summary>
/// A class for games supported on CnCNet (DTA, TI, TS, RA1/2, etc.)
/// </summary>
public class CnCNetGame
{
    /// <summary>
    /// Gets or sets the name of the game that is displayed on the user-interface.
    /// </summary>
    public string UIName { get; set; }

    /// <summary>
    /// Gets or sets the internal name (suffix) of the game.
    /// </summary>
    public string InternalName { get; set; }

    /// <summary>
    /// Gets or sets the IRC chat channel ID of the game.
    /// </summary>
    public string ChatChannel { get; set; }

    /// <summary>
    /// Gets or sets the IRC game broadcasting channel ID of the game.
    /// </summary>
    public string GameBroadcastChannel { get; set; }

    /// <summary>
    /// Gets or sets the executable name of the game's client.
    /// </summary>
    public string ClientExecutableName { get; set; }

    public Texture2D Texture { get; set; }

    /// <summary>
    /// Gets or sets the location where to read the game's installation path from the registry.
    /// </summary>
    public string RegistryInstallPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether determines if the game is properly supported by this client.
    /// Defaults to true.
    /// </summary>
    public bool Supported { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether if true, the client should always be connected to this game's chat channel.
    /// </summary>
    public bool AlwaysEnabled { get; set; }
}