using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClientCore.Properties;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace ClientCore.CnCNet5;

/// <summary>
/// A class for storing the collection of supported CnCNet games.
/// </summary>
public class GameCollection : ICollection<CnCNetGame>
{
    public int Count => ((ICollection<CnCNetGame>)GameList).Count;

    public List<CnCNetGame> GameList { get; private set; }

    public bool IsReadOnly => ((ICollection<CnCNetGame>)GameList).IsReadOnly;

    public void Add(CnCNetGame item)
    {
        ((ICollection<CnCNetGame>)GameList).Add(item);
    }

    public void Clear()
    {
        ((ICollection<CnCNetGame>)GameList).Clear();
    }

    public bool Contains(CnCNetGame item)
    {
        return ((ICollection<CnCNetGame>)GameList).Contains(item);
    }

    public void CopyTo(CnCNetGame[] array, int arrayIndex)
    {
        ((ICollection<CnCNetGame>)GameList).CopyTo(array, arrayIndex);
    }

    public IEnumerator<CnCNetGame> GetEnumerator()
    {
        return ((IEnumerable<CnCNetGame>)GameList).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)GameList).GetEnumerator();
    }

    /// <summary>
    /// Returns the full UI name of a game based on its index in the game list.
    /// </summary>
    /// <param name="gameIndex">The index of the CnCNet supported game.</param>
    /// <returns>The UI name of the game.</returns>
    public string GetFullGameNameFromIndex(int gameIndex)
    {
        return GameList[gameIndex].UIName;
    }

    public string GetGameBroadcastingChannelNameFromIdentifier(string gameIdentifier)
    {
        CnCNetGame game = GameList.Find(g => g.InternalName == gameIdentifier.ToLowerInvariant());
        if (game == null)
            return null;
        return game.GameBroadcastChannel;
    }

    public string GetGameChatChannelNameFromIdentifier(string gameIdentifier)
    {
        CnCNetGame game = GameList.Find(g => g.InternalName == gameIdentifier.ToLowerInvariant());
        if (game == null)
            return null;
        return game.ChatChannel;
    }

    /// <summary>
    /// Returns the internal name of a game based on its index in the game list.
    /// </summary>
    /// <param name="gameIndex">The index of the CnCNet supported game.</param>
    /// <returns>The internal name (suffix) of the game.</returns>
    public string GetGameIdentifierFromIndex(int gameIndex)
    {
        return GameList[gameIndex].InternalName;
    }

    /// <summary>
    /// Gets the index of a CnCNet supported game based on its internal name.
    /// </summary>
    /// <param name="gameName">The internal name (suffix) of the game.</param>
    /// <returns>The index of the specified CnCNet game. -1 if the game is unknown or not supported.</returns>
    public int GetGameIndexFromInternalName(string gameName)
    {
        for (int gId = 0; gId < GameList.Count; gId++)
        {
            CnCNetGame game = GameList[gId];

            if (gameName.ToLowerInvariant() == game.InternalName)
                return gId;
        }

        return -1;
    }

    /// <summary>
    /// Seeks the supported game list for a specific game's internal name and if found, returns the
    /// game's full name. Otherwise returns the internal name specified in the param.
    /// </summary>
    /// <param name="gameName">The internal name of the game to seek for.</param>
    /// <returns>
    /// The full name of a supported game based on its internal name. Returns the given parameter if
    /// the name isn't found in the supported game list.
    /// </returns>
    public string GetGameNameFromInternalName(string gameName)
    {
        CnCNetGame game = GameList.Find(g => g.InternalName == gameName.ToLowerInvariant());

        if (game == null)
            return gameName;

        return game.UIName;
    }

    public void Initialize()
    {
        GameList = new List<CnCNetGame>();

        // Default supported games.
        CnCNetGame[] defaultGames = new[]
        {
            new CnCNetGame()
            {
                ChatChannel = "#cncnet-dta",
                ClientExecutableName = "DTA.exe",
                GameBroadcastChannel = "#cncnet-dta-games",
                InternalName = "dta",
                RegistryInstallPath = "HKCU\\Software\\TheDawnOfTheTiberiumAge",
                UIName = "Dawn of the Tiberium Age",
                Texture = AssetLoader.TextureFromImage(Resources.dtaicon)
            },

            new CnCNetGame()
            {
                ChatChannel = "#cncnet-ti",
                ClientExecutableName = "TI_Launcher.exe",
                GameBroadcastChannel = "#cncnet-ti-games",
                InternalName = "ti",
                RegistryInstallPath = "HKCU\\Software\\TwistedInsurrection",
                UIName = "Twisted Insurrection",
                Texture = AssetLoader.TextureFromImage(Resources.tiicon)
            },

            new CnCNetGame()
            {
                ChatChannel = "#cncnet-ts",
                ClientExecutableName = "TiberianSun.exe",
                GameBroadcastChannel = "#cncnet-ts-games",
                InternalName = "ts",
                RegistryInstallPath = "HKLM\\Software\\Westwood\\Tiberian Sun",
                UIName = "Tiberian Sun",
                Texture = AssetLoader.TextureFromImage(Resources.tsicon)
            },

            new CnCNetGame()
            {
                ChatChannel = "#cncnet-mo",
                ClientExecutableName = "MentalOmegaClient.exe",
                GameBroadcastChannel = "#cncnet-mo-games",
                InternalName = "mo",
                RegistryInstallPath = "HKCU\\Software\\MentalOmega",
                UIName = "Mental Omega",
                Texture = AssetLoader.TextureFromImage(Resources.moicon)
            },

            new CnCNetGame()
            {
                ChatChannel = "#cncnet-yr",
                ClientExecutableName = "CnCNetClientYR.exe",
                GameBroadcastChannel = "#cncnet-yr-games",
                InternalName = "yr",
                RegistryInstallPath = "HKLM\\Software\\Westwood\\Yuri's Revenge",
                UIName = "Yuri's Revenge",
                Texture = AssetLoader.TextureFromImage(Resources.yricon)
            },

            new CnCNetGame()
            {
                ChatChannel = "#redres-lobby",
                ClientExecutableName = "RRLauncher.exe",
                GameBroadcastChannel = "#redres-games",
                InternalName = "rr",
                RegistryInstallPath = "HKML\\Software\\RedResurrection",
                UIName = "YR Red-Resurrection",
                Texture = AssetLoader.TextureFromImage(Resources.rricon)
            },

            new CnCNetGame()
            {
                ChatChannel = "#cncreloaded",
                ClientExecutableName = "CnCReloadedClient.exe",
                GameBroadcastChannel = "#cncreloaded-games",
                InternalName = "cncr",
                RegistryInstallPath = "HKCU\\Software\\CnCReloaded",
                UIName = "C&C: Reloaded",
                Texture = AssetLoader.TextureFromImage(Resources.cncricon)
            }
        };

        // CnCNet chat + unsupported games.
        CnCNetGame[] otherGames = new[]
        {
            new CnCNetGame()
            {
                ChatChannel = "#cncnet",
                InternalName = "cncnet",
                UIName = "General CnCNet Chat",
                AlwaysEnabled = true,
                Texture = AssetLoader.TextureFromImage(Resources.cncneticon)
            },

            new CnCNetGame()
            {
                ChatChannel = "#cncnet-td",
                InternalName = "td",
                UIName = "Tiberian Dawn",
                Supported = false,
                Texture = AssetLoader.TextureFromImage(Resources.tdicon)
            },

            new CnCNetGame()
            {
                ChatChannel = "#cncnet-ra",
                InternalName = "ra",
                UIName = "Red Alert",
                Supported = false,
                Texture = AssetLoader.TextureFromImage(Resources.raicon)
            },

            new CnCNetGame()
            {
                ChatChannel = "#cncnet-d2",
                InternalName = "d2",
                UIName = "Dune 2000",
                Supported = false,
                Texture = AssetLoader.TextureFromImage(Resources.unknownicon)
            }
        };

        GameList.AddRange(defaultGames);
        GameList.AddRange(GameCollection.GetCustomGames(defaultGames.Concat(otherGames).ToList()));
        GameList.AddRange(otherGames);

        if (GetGameIndexFromInternalName(ClientConfiguration.Instance.LocalGame) == -1)
        {
            throw new ClientConfigurationException("Could not find a game in the game collection matching LocalGame value of " +
                ClientConfiguration.Instance.LocalGame + ".");
        }
    }

    public bool Remove(CnCNetGame item)
    {
        return ((ICollection<CnCNetGame>)GameList).Remove(item);
    }

    private static List<CnCNetGame> GetCustomGames(List<CnCNetGame> existingGames)
    {
        IniFile iniFile = new(ProgramConstants.GetBaseResourcePath() + "GameCollectionConfig.ini");

        List<CnCNetGame> customGames = new();

        IniSection section = iniFile.GetSection("CustomGames");

        if (section == null)
            return customGames;

        HashSet<string> customGameIDs = new();
        foreach (KeyValuePair<string, string> kvp in section.Keys)
        {
            if (!iniFile.SectionExists(kvp.Value))
                continue;

            string id = iniFile.GetStringValue(kvp.Value, "InternalName", string.Empty).ToLower();

            if (string.IsNullOrEmpty(id))
                throw new GameCollectionConfigurationException("InternalName for game " + kvp.Value + " is not defined or set to an empty value.");

            if (id.Length > ProgramConstants.GAMEIDMAXLENGTH)
            {
                throw new GameCollectionConfigurationException("InternalGame for game " + kvp.Value + " is set to a value that exceeds length limit of " +
                    ProgramConstants.GAMEIDMAXLENGTH + " characters.");
            }

            if (existingGames.Find(g => g.InternalName == id) != null || customGameIDs.Contains(id))
                throw new GameCollectionConfigurationException("Game with InternalName " + id.ToUpper() + " already exists in the game collection.");

            string iconFilename = iniFile.GetStringValue(kvp.Value, "IconFilename", id + "icon.png");
            customGames.Add(new CnCNetGame
            {
                InternalName = id,
                UIName = iniFile.GetStringValue(kvp.Value, "UIName", id.ToUpper()),
                ChatChannel = GetIRCChannelNameFromIniFile(iniFile, kvp.Value, "ChatChannel"),
                GameBroadcastChannel = GetIRCChannelNameFromIniFile(iniFile, kvp.Value, "GameBroadcastChannel"),
                ClientExecutableName = iniFile.GetStringValue(kvp.Value, "ClientExecutableName", string.Empty),
                RegistryInstallPath = iniFile.GetStringValue(kvp.Value, "RegistryInstallPath", $"HKCU\\Software\\{id.ToUpper()}"),
                Texture = AssetLoader.AssetExists(iconFilename) ? AssetLoader.LoadTexture(iconFilename) :
                AssetLoader.TextureFromImage(Resources.unknownicon)
            });
            _ = customGameIDs.Add(id);
        }

        return customGames;
    }

    private static string GetIRCChannelNameFromIniFile(IniFile iniFile, string section, string key)
    {
        string channel = iniFile.GetStringValue(section, key, string.Empty);

        if (string.IsNullOrEmpty(channel))
            throw new GameCollectionConfigurationException(key + " for game " + section + " is not defined or set to an empty value.");

        if (channel.Contains(' ') || channel.Contains(',') || channel.Contains((char)7))
            throw new GameCollectionConfigurationException(key + " for game " + section + " contains characters not allowed on IRC channel names.");

        if (!channel.StartsWith("#"))
            return "#" + channel;

        return channel;
    }
}