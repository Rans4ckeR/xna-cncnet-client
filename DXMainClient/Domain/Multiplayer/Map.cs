using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClientCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Utilities = Rampastring.Tools.Utilities;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// A multiplayer map.
/// </summary>
public class Map
{
    private const int MAX_PLAYERS = 8;

    [JsonProperty]
    private readonly string customMapFilePath;

    private readonly List<ExtraMapPreviewTexture> extraTextures = new(0);

    private readonly List<KeyValuePair<string, string>> forcedSpawnIniOptions = new(0);

    [JsonProperty]
    private readonly List<string> waypoints = new();

    /// <summary>
    /// The forced UnitCount for the map. -1 means none.
    /// </summary>
    [JsonProperty]
    private int unitCount = -1;

    /// <summary>
    /// The forced starting credits for the map. -1 means none.
    /// </summary>
    [JsonProperty]
    private int credits = -1;

    [JsonProperty]
    private int neutralHouseColor = -1;

    [JsonProperty]
    private int specialHouseColor = -1;

    [JsonProperty]
    private int bases = -1;

    [JsonProperty]
    private string[] localSize;

    [JsonProperty]
    private string[] actualSize;

    private IniFile customMapIni;

    /// <summary>
    /// The pixel coordinates of the map's player starting locations.
    /// </summary>
    [JsonProperty]
    private List<Point> startingLocations;

    public Map(string baseFilePath, string customMapFilePath = null)
    {
        BaseFilePath = baseFilePath;
        this.customMapFilePath = customMapFilePath;
        Official = string.IsNullOrEmpty(this.customMapFilePath);
    }

    /// <summary>
    /// Gets or sets the game modes that the map is listed for.
    /// </summary>
    [JsonProperty]
    public string[] GameModes { get; set; }

    [JsonProperty]
    public List<TeamStartMappingPreset> TeamStartMappingPresets { get; set; } = new();

    /// <summary>
    /// Gets the name of the map.
    /// </summary>
    [JsonProperty]
    public string Name { get; private set; }

    /// <summary>
    /// Gets the maximum amount of players supported by the map.
    /// </summary>
    [JsonProperty]
    public int MaxPlayers { get; private set; }

    /// <summary>
    /// Gets the minimum amount of players supported by the map.
    /// </summary>
    [JsonProperty]
    public int MinPlayers { get; private set; }

    /// <summary>
    /// Gets a value indicating whether whether to use MaxPlayers for limiting the player count of
    /// the map. If false (which is the default), MaxPlayers is only used for randomizing players to
    /// starting waypoints.
    /// </summary>
    [JsonProperty]
    public bool EnforceMaxPlayers { get; private set; }

    /// <summary>
    /// Gets a value indicating whether controls if the map is meant for a co-operation game mode
    /// (enables briefing logic and forcing options, among others).
    /// </summary>
    [JsonProperty]
    public bool IsCoop { get; private set; }

    /// <summary>
    /// Gets a value indicating whether if set, this map won't be automatically transferred over
    /// CnCNet when a player doesn't have it.
    /// </summary>
    [JsonIgnore]
    public bool Official { get; private set; }

    /// <summary>
    /// Gets contains co-op information.
    /// </summary>
    [JsonProperty]
    public CoopMapInfo CoopInfo { get; private set; }

    /// <summary>
    /// Gets the briefing of the map.
    /// </summary>
    [JsonProperty]
    public string Briefing { get; private set; }

    /// <summary>
    /// Gets the author of the map.
    /// </summary>
    [JsonProperty]
    public string Author { get; private set; }

    /// <summary>
    /// Gets the calculated SHA1 of the map.
    /// </summary>
    [JsonIgnore]
    public string SHA1 { get; private set; }

    /// <summary>
    /// Gets the path to the map file.
    /// </summary>
    [JsonProperty]
    public string BaseFilePath { get; private set; }

    /// <summary>
    /// Gets the complete path to the map file. Includes the game directory in the path.
    /// </summary>
    public string CompleteFilePath => ProgramConstants.GamePath + BaseFilePath + ".map";

    /// <summary>
    /// Gets the file name of the preview image.
    /// </summary>
    [JsonProperty]
    public string PreviewPath { get; private set; }

    /// <summary>
    /// Gets a value indicating whether if set, this map cannot be played on Skirmish.
    /// </summary>
    [JsonProperty]
    public bool MultiplayerOnly { get; private set; }

    /// <summary>
    /// Gets a value indicating whether if set, this map cannot be played with AI players.
    /// </summary>
    [JsonProperty]
    public bool HumanPlayersOnly { get; private set; }

    /// <summary>
    /// Gets a value indicating whether if set, players are forced to random starting locations on
    /// this map.
    /// </summary>
    [JsonProperty]
    public bool ForceRandomStartLocations { get; private set; }

    /// <summary>
    /// Gets a value indicating whether if set, players are forced to different teams on this map.
    /// </summary>
    [JsonProperty]
    public bool ForceNoTeams { get; private set; }

    /// <summary>
    /// Gets the name of an extra INI file in INI\Map Code\ that should be embedded into this map's
    /// INI code when a game is started.
    /// </summary>
    [JsonProperty]
    public string ExtraININame { get; private set; }

    public List<KeyValuePair<string, bool>> ForcedCheckBoxValues { get; set; } = new(0);

    [JsonIgnore]
    public List<TeamStartMapping> TeamStartMappings => TeamStartMappingPresets?.FirstOrDefault()?.TeamStartMappings;

    public Texture2D PreviewTexture { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether if false, the preview shouldn't be extracted for
    /// this (custom) map.
    /// </summary>
    public bool ExtractCustomPreview { get; set; } = true;

    public List<KeyValuePair<string, int>> ForcedDropDownValues { get; set; } = new(0);

    public void CalculateSHA()
    {
        SHA1 = Utilities.CalculateSHA1ForFile(CompleteFilePath);
    }

    public List<ExtraMapPreviewTexture> GetExtraMapPreviewTextures() => extraTextures;

    /// <summary>
    /// This is used to load a map from the MPMaps.ini (default name) file.
    /// </summary>
    /// <param name="iniFile">ini file.</param>
    /// <returns>result.</returns>
    public bool SetInfoFromMpMapsINI(IniFile iniFile)
    {
        try
        {
            string baseSectionName = iniFile.GetStringValue(BaseFilePath, "BaseSection", string.Empty);

            if (!string.IsNullOrEmpty(baseSectionName))
                iniFile.CombineSections(baseSectionName, BaseFilePath);

            IniSection section = iniFile.GetSection(BaseFilePath);

            Name = section.GetStringValue("Description", "Unnamed map");
            Author = section.GetStringValue("Author", "Unknown author");
            GameModes = section.GetStringValue("GameModes", "Default").Split(',');

            MinPlayers = section.GetIntValue("MinPlayers", 0);
            MaxPlayers = section.GetIntValue("MaxPlayers", 0);
            EnforceMaxPlayers = section.GetBooleanValue("EnforceMaxPlayers", false);
            PreviewPath = Path.GetDirectoryName(BaseFilePath) + "/" +
                section.GetStringValue("PreviewImage", Path.GetFileNameWithoutExtension(BaseFilePath) + ".png");
            Briefing = section.GetStringValue("Briefing", string.Empty).Replace("@", Environment.NewLine);
            CalculateSHA();
            IsCoop = section.GetBooleanValue("IsCoopMission", false);
            credits = section.GetIntValue("Credits", -1);
            unitCount = section.GetIntValue("UnitCount", -1);
            neutralHouseColor = section.GetIntValue("NeutralColor", -1);
            specialHouseColor = section.GetIntValue("SpecialColor", -1);
            MultiplayerOnly = section.GetBooleanValue("MultiplayerOnly", false);
            HumanPlayersOnly = section.GetBooleanValue("HumanPlayersOnly", false);
            ForceRandomStartLocations = section.GetBooleanValue("ForceRandomStartLocations", false);
            ForceNoTeams = section.GetBooleanValue("ForceNoTeams", false);
            ExtraININame = section.GetStringValue("ExtraININame", string.Empty);
            string bases = section.GetStringValue("Bases", string.Empty);
            if (!string.IsNullOrEmpty(bases))
            {
                this.bases = Convert.ToInt32(Conversions.BooleanFromString(bases, false));
            }

            int i = 0;
            while (true)
            {
                // Format example: ExtraTexture0=oilderrick.png,200,150,1,false Third value is
                // optional map cell level, defaults to 0 if unspecified. Fourth value is optional
                // boolean value that determines if the texture can be toggled on / off.
                string value = section.GetStringValue("ExtraTexture" + i, null);

                if (string.IsNullOrWhiteSpace(value))
                    break;

                string[] parts = value.Split(',');

                if (parts.Length is < 3 or > 5)
                {
                    Logger.Log($"Invalid format for ExtraTexture{i} in map " + BaseFilePath);
                    continue;
                }

                bool success = int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x);
                success &= int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y);

                int level = 0;
                bool toggleable = false;

                if (parts.Length > 3)
                    int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out level);

                if (parts.Length > 4)
                    toggleable = Conversions.BooleanFromString(parts[4], false);

                extraTextures.Add(new ExtraMapPreviewTexture(parts[0], new Point(x, y), level, toggleable));

                i++;
            }

            if (IsCoop)
            {
                CoopInfo = new CoopMapInfo();
                string[] disallowedSides = section.GetStringValue("DisallowedPlayerSides", string.Empty).Split(
                    new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string sideIndex in disallowedSides)
                    CoopInfo.DisallowedPlayerSides.Add(int.Parse(sideIndex));

                string[] disallowedColors = section.GetStringValue("DisallowedPlayerColors", string.Empty).Split(
                    new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string colorIndex in disallowedColors)
                    CoopInfo.DisallowedPlayerColors.Add(int.Parse(colorIndex));

                CoopInfo.SetHouseInfos(section);
            }

            localSize = section.GetStringValue("LocalSize", "0,0,0,0").Split(',');
            actualSize = section.GetStringValue("Size", "0,0,0,0").Split(',');

            for (i = 0; i < MAX_PLAYERS; i++)
            {
                string waypoint = section.GetStringValue("Waypoint" + i, string.Empty);

                if (string.IsNullOrEmpty(waypoint))
                    break;

                waypoints.Add(waypoint);
            }

            GetTeamStartMappingPresets(section);
#if !WINDOWSGL

            if (UserINISettings.Instance.PreloadMapPreviews)
                PreviewTexture = LoadPreviewTexture();
#endif

            // Parse forced options
            string forcedOptionsSections = iniFile.GetStringValue(BaseFilePath, "ForcedOptions", string.Empty);

            if (!string.IsNullOrEmpty(forcedOptionsSections))
            {
                string[] sections = forcedOptionsSections.Split(',');
                foreach (string foSection in sections)
                    ParseForcedOptions(iniFile, foSection);
            }

            string forcedSpawnIniOptionsSections = iniFile.GetStringValue(BaseFilePath, "ForcedSpawnIniOptions", string.Empty);

            if (!string.IsNullOrEmpty(forcedSpawnIniOptionsSections))
            {
                string[] sections = forcedSpawnIniOptionsSections.Split(',');
                foreach (string fsioSection in sections)
                    ParseSpawnIniOptions(iniFile, fsioSection);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("Setting info for " + BaseFilePath + " failed! Reason: " + ex.Message);
            return false;
        }
    }

    public List<Point> GetStartingLocationPreviewCoords(Point previewSize)
    {
        if (startingLocations == null)
        {
            startingLocations = new List<Point>();

            foreach (string waypoint in waypoints)
            {
                startingLocations.Add(GetWaypointCoords(waypoint, actualSize, localSize, previewSize));
            }
        }

        return startingLocations;
    }

    public Point MapPointToMapPreviewPoint(Point mapPoint, Point previewSize, int level)
    {
        return GetIsoTilePixelCoord(mapPoint.X, mapPoint.Y, actualSize, localSize, previewSize, level);
    }

    /// <summary>
    /// Loads map information from a TS/RA2 map INI file. Returns true if successful, otherwise false.
    /// </summary>
    /// <returns>result.</returns>
    public bool SetInfoFromCustomMap()
    {
        if (!File.Exists(customMapFilePath))
            return false;

        try
        {
            IniFile iniFile = GetCustomMapIniFile();

            IniSection basicSection = iniFile.GetSection("Basic");

            Name = basicSection.GetStringValue("Name", "Unnamed map");
            Author = basicSection.GetStringValue("Author", "Unknown author");

            string gameModesString = basicSection.GetStringValue("GameModes", string.Empty);
            if (string.IsNullOrEmpty(gameModesString))
            {
                gameModesString = basicSection.GetStringValue("GameMode", "Default");
            }

            GameModes = gameModesString.Split(',');

            if (GameModes.Length == 0)
            {
                Logger.Log("Custom map " + customMapFilePath + " has no game modes!");
                return false;
            }

            for (int i = 0; i < GameModes.Length; i++)
            {
                string gameMode = GameModes[i].Trim();
                GameModes[i] = gameMode.Substring(0, 1).ToUpperInvariant() + gameMode.Substring(1);
            }

            MinPlayers = 0;
            MaxPlayers = basicSection.KeyExists("ClientMaxPlayer")
                ? basicSection.GetIntValue("ClientMaxPlayer", 0)
                : basicSection.GetIntValue("MaxPlayer", 0);
            EnforceMaxPlayers = basicSection.GetBooleanValue("EnforceMaxPlayers", true);

            //PreviewPath = Path.GetDirectoryName(BaseFilePath) + "/" +
            //    iniFile.GetStringValue(BaseFilePath, "PreviewImage", Path.GetFileNameWithoutExtension(BaseFilePath) + ".png");
            Briefing = basicSection.GetStringValue("Briefing", string.Empty).Replace("@", Environment.NewLine);
            CalculateSHA();
            IsCoop = basicSection.GetBooleanValue("IsCoopMission", false);
            credits = basicSection.GetIntValue("Credits", -1);
            unitCount = basicSection.GetIntValue("UnitCount", -1);
            neutralHouseColor = basicSection.GetIntValue("NeutralColor", -1);
            specialHouseColor = basicSection.GetIntValue("SpecialColor", -1);
            HumanPlayersOnly = basicSection.GetBooleanValue("HumanPlayersOnly", false);
            ForceRandomStartLocations = basicSection.GetBooleanValue("ForceRandomStartLocations", false);
            ForceNoTeams = basicSection.GetBooleanValue("ForceNoTeams", false);
            PreviewPath = Path.ChangeExtension(customMapFilePath.Substring(ProgramConstants.GamePath.Length), ".png");
            MultiplayerOnly = basicSection.GetBooleanValue("ClientMultiplayerOnly", false);

            string bases = basicSection.GetStringValue("Bases", string.Empty);
            if (!string.IsNullOrEmpty(bases))
            {
                this.bases = Convert.ToInt32(Conversions.BooleanFromString(bases, false));
            }

            if (IsCoop)
            {
                CoopInfo = new CoopMapInfo();
                string[] disallowedSides = iniFile.GetStringValue("Basic", "DisallowedPlayerSides", string.Empty).Split(
                    new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string sideIndex in disallowedSides)
                    CoopInfo.DisallowedPlayerSides.Add(int.Parse(sideIndex));

                string[] disallowedColors = iniFile.GetStringValue("Basic", "DisallowedPlayerColors", string.Empty).Split(
                    new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string colorIndex in disallowedColors)
                    CoopInfo.DisallowedPlayerColors.Add(int.Parse(colorIndex));

                CoopInfo.SetHouseInfos(basicSection);
            }

            localSize = iniFile.GetStringValue("Map", "LocalSize", "0,0,0,0").Split(',');
            actualSize = iniFile.GetStringValue("Map", "Size", "0,0,0,0").Split(',');

            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                string waypoint = GetCustomMapIniFile().GetStringValue("Waypoints", i.ToString(), string.Empty);

                if (string.IsNullOrEmpty(waypoint))
                    break;

                waypoints.Add(waypoint);
            }

            GetTeamStartMappingPresets(basicSection);

            ParseForcedOptions(iniFile, "ForcedOptions");
            ParseSpawnIniOptions(iniFile, "ForcedSpawnIniOptions");

            return true;
        }
        catch
        {
            Logger.Log("Loading custom map " + customMapFilePath + " failed!");
            return false;
        }
    }

    /// <summary>
    /// Loads and returns the map preview texture.
    /// </summary>
    /// <returns>result.</returns>
    public Texture2D LoadPreviewTexture()
    {
        if (File.Exists(ProgramConstants.GamePath + PreviewPath))
            return AssetLoader.LoadTextureUncached(PreviewPath);

        if (!Official)
        {
            // Extract preview from the map itself
            System.Drawing.Bitmap preview = MapPreviewExtractor.ExtractMapPreview(GetCustomMapIniFile());

            if (preview != null)
            {
                Texture2D texture = AssetLoader.TextureFromImage(preview);
                if (texture != null)
                    return texture;
            }
        }

        return AssetLoader.CreateTexture(Color.Black, 10, 10);
    }

    public IniFile GetMapIni()
    {
        IniFile mapIni = new(CompleteFilePath);

        if (!string.IsNullOrEmpty(ExtraININame))
        {
            IniFile extraIni = new(ProgramConstants.GamePath + "INI/Map Code/" + ExtraININame);
            IniFile.ConsolidateIniFiles(mapIni, extraIni);
        }

        return mapIni;
    }

    public void ApplySpawnIniCode(
        IniFile spawnIni,
        int totalPlayerCount,
        int aiPlayerCount,
        int coopDifficultyLevel)
    {
        foreach (KeyValuePair<string, string> key in forcedSpawnIniOptions)
            spawnIni.SetStringValue("Settings", key.Key, key.Value);

        if (credits != -1)
            spawnIni.SetIntValue("Settings", "Credits", credits);

        if (unitCount != -1)
            spawnIni.SetIntValue("Settings", "UnitCount", unitCount);

        int neutralHouseIndex = totalPlayerCount + 1;
        int specialHouseIndex = totalPlayerCount + 2;

        if (IsCoop)
        {
            List<CoopHouseInfo> allyHouses = CoopInfo.AllyHouses;
            List<CoopHouseInfo> enemyHouses = CoopInfo.EnemyHouses;

            int multiId = totalPlayerCount + 1;
            foreach (CoopHouseInfo houseInfo in allyHouses.Concat(enemyHouses))
            {
                spawnIni.SetIntValue("HouseHandicaps", "Multi" + multiId, coopDifficultyLevel);
                spawnIni.SetIntValue("HouseCountries", "Multi" + multiId, houseInfo.Side);
                spawnIni.SetIntValue("HouseColors", "Multi" + multiId, houseInfo.Color);
                spawnIni.SetIntValue("SpawnLocations", "Multi" + multiId, houseInfo.StartingLocation);

                multiId++;
            }

            for (int i = 0; i < allyHouses.Count; i++)
            {
                int aMultiId = totalPlayerCount + i + 1;

                int allyIndex = 0;

                // Write alliances
                for (int pIndex = 0; pIndex < totalPlayerCount + allyHouses.Count; pIndex++)
                {
                    int allyMultiIndex = pIndex;

                    if (pIndex == aMultiId - 1)
                        continue;

                    spawnIni.SetIntValue(
                        "Multi" + aMultiId + "_Alliances",
                        "HouseAlly" + HouseAllyIndexToString(allyIndex),
                        allyMultiIndex);
                    spawnIni.SetIntValue(
                        "Multi" + (allyMultiIndex + 1) + "_Alliances",
                        "HouseAlly" + HouseAllyIndexToString(totalPlayerCount + i - 1),
                        aMultiId - 1);
                    allyIndex++;
                }
            }

            for (int i = 0; i < enemyHouses.Count; i++)
            {
                int eMultiId = totalPlayerCount + allyHouses.Count + i + 1;

                int allyIndex = 0;

                // Write alliances
                for (int enemyIndex = 0; enemyIndex < enemyHouses.Count; enemyIndex++)
                {
                    int allyMultiIndex = totalPlayerCount + allyHouses.Count + enemyIndex;

                    if (enemyIndex == i)
                        continue;

                    spawnIni.SetIntValue(
                        "Multi" + eMultiId + "_Alliances",
                        "HouseAlly" + HouseAllyIndexToString(allyIndex),
                        allyMultiIndex);
                    allyIndex++;
                }
            }

            spawnIni.SetIntValue(
                "Settings",
                "AIPlayers",
                aiPlayerCount + allyHouses.Count + enemyHouses.Count);

            neutralHouseIndex += allyHouses.Count + enemyHouses.Count;
            specialHouseIndex += allyHouses.Count + enemyHouses.Count;
        }

        if (neutralHouseColor > -1)
            spawnIni.SetIntValue("HouseColors", "Multi" + neutralHouseIndex, neutralHouseColor);

        if (specialHouseColor > -1)
            spawnIni.SetIntValue("HouseColors", "Multi" + specialHouseIndex, specialHouseColor);

        if (bases > -1)
            spawnIni.SetBooleanValue("Settings", "Bases", Convert.ToBoolean(bases));
    }

    public string GetSizeString()
    {
        if (actualSize == null || actualSize.Length < 4)
            return "Not available";
        return actualSize[2] + "x" + actualSize[3];
    }

    public override int GetHashCode() => SHA1 != null ? SHA1.GetHashCode() : 0;

    protected bool Equals(Map other) => string.Equals(SHA1, other?.SHA1, StringComparison.OrdinalIgnoreCase);

    private static string HouseAllyIndexToString(int index)
    {
        string[] houseAllyIndexStrings = new string[]
        {
            "One",
            "Two",
            "Three",
            "Four",
            "Five",
            "Six",
            "Seven"
        };

        return houseAllyIndexStrings[index];
    }

    /// <summary>
    /// Converts a waypoint's coordinate string into pixel coordinates on the preview image.
    /// </summary>
    /// <returns>The waypoint's location on the map preview as a point.</returns>
    private static Point GetWaypointCoords(
        string waypoint,
        string[] actualSizeValues,
        string[] localSizeValues,
        Point previewSizePoint)
    {
        string[] parts = waypoint.Split(',');

        int xCoordIndex = parts[0].Length - 3;

        int isoTileY = Convert.ToInt32(parts[0].Substring(0, xCoordIndex));
        int isoTileX = Convert.ToInt32(parts[0].Substring(xCoordIndex));

        int level = 0;

        if (parts.Length > 1)
            level = Conversions.IntFromString(parts[1], 0);

        return GetIsoTilePixelCoord(isoTileX, isoTileY, actualSizeValues, localSizeValues, previewSizePoint, level);
    }

    private static Point GetIsoTilePixelCoord(int isoTileX, int isoTileY, string[] actualSizeValues, string[] localSizeValues, Point previewSizePoint, int level)
    {
        int rx = isoTileX - isoTileY + Convert.ToInt32(actualSizeValues[2]) - 1;
        int ry = isoTileX + isoTileY - Convert.ToInt32(actualSizeValues[2]) - 1;

        int pixelPosX = rx * MainClientConstants.MapCellSizeX / 2;
        int pixelPosY = (ry * MainClientConstants.MapCellSizeY / 2) - (level * MainClientConstants.MapCellSizeY / 2);

        pixelPosX -= Convert.ToInt32(localSizeValues[0]) * MainClientConstants.MapCellSizeX;
        pixelPosY -= Convert.ToInt32(localSizeValues[1]) * MainClientConstants.MapCellSizeY;

        // Calculate map size
        int mapSizeX = Convert.ToInt32(localSizeValues[2]) * MainClientConstants.MapCellSizeX;
        int mapSizeY = Convert.ToInt32(localSizeValues[3]) * MainClientConstants.MapCellSizeY;

        double ratioX = Convert.ToDouble(pixelPosX) / mapSizeX;
        double ratioY = Convert.ToDouble(pixelPosY) / mapSizeY;

        int pixelX = Convert.ToInt32(ratioX * previewSizePoint.X);
        int pixelY = Convert.ToInt32(ratioY * previewSizePoint.Y);

        return new Point(pixelX, pixelY);
    }

    private void GetTeamStartMappingPresets(IniSection section)
    {
        TeamStartMappingPresets = new List<TeamStartMappingPreset>();
        for (int i = 0; ; i++)
        {
            try
            {
                string teamStartMappingPreset = section.GetStringValue($"TeamStartMapping{i}", string.Empty);
                if (string.IsNullOrEmpty(teamStartMappingPreset))
                    return; // mapping not found

                string teamStartMappingPresetName = section.GetStringValue($"TeamStartMapping{i}Name", string.Empty);
                if (string.IsNullOrEmpty(teamStartMappingPresetName))
                    continue; // mapping found, but no name specified

                TeamStartMappingPresets.Add(new TeamStartMappingPreset()
                {
                    Name = teamStartMappingPresetName,
                    TeamStartMappings = TeamStartMapping.FromListString(teamStartMappingPreset)
                });
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to parse team start mappings. Map: \"{Name}\", Error: {e.Message}");
                TeamStartMappingPresets = new List<TeamStartMappingPreset>();
            }
        }
    }

    /// <summary>
    /// Due to caching, this may not have been loaded on application start. This function provides
    /// the ability to load when needed.
    /// </summary>
    /// <returns>result.</returns>
    private IniFile GetCustomMapIniFile()
    {
        if (customMapIni != null)
            return customMapIni;

        customMapIni = new IniFile { FileName = customMapFilePath };
        customMapIni.AddSection("Basic");
        customMapIni.AddSection("Map");
        customMapIni.AddSection("Waypoints");
        customMapIni.AddSection("Preview");
        customMapIni.AddSection("PreviewPack");
        customMapIni.AddSection("ForcedOptions");
        customMapIni.AddSection("ForcedSpawnIniOptions");
        customMapIni.AllowNewSections = false;
        customMapIni.Parse();

        return customMapIni;
    }

    private void ParseForcedOptions(IniFile iniFile, string forcedOptionsSection)
    {
        List<string> keys = iniFile.GetSectionKeys(forcedOptionsSection);

        if (keys == null)
        {
            Logger.Log("Invalid ForcedOptions section \"" + forcedOptionsSection + "\" in map " + BaseFilePath);
            return;
        }

        foreach (string key in keys)
        {
            string value = iniFile.GetStringValue(forcedOptionsSection, key, string.Empty);

            if (int.TryParse(value, out int intValue))
            {
                ForcedDropDownValues.Add(new KeyValuePair<string, int>(key, intValue));
            }
            else
            {
                ForcedCheckBoxValues.Add(new KeyValuePair<string, bool>(key, Conversions.BooleanFromString(value, false)));
            }
        }
    }

    private void ParseSpawnIniOptions(IniFile forcedOptionsIni, string spawnIniOptionsSection)
    {
        List<string> spawnIniKeys = forcedOptionsIni.GetSectionKeys(spawnIniOptionsSection);

        foreach (string key in spawnIniKeys)
        {
            forcedSpawnIniOptions.Add(new KeyValuePair<string, string>(
                key,
                forcedOptionsIni.GetStringValue(spawnIniOptionsSection, key, string.Empty)));
        }
    }
}