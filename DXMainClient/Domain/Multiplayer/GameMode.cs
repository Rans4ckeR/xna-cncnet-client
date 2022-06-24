﻿using System;
using System.Collections.Generic;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// A multiplayer game mode.
/// </summary>
public class GameMode
{
    /// <summary>
    /// List of side indices players cannot select in this game mode.
    /// </summary>
    public List<int> DisallowedPlayerSides = new();

    public List<Map> Maps = new();

    private const string BASE_INI_PATH = "INI/Map Code/";

    public GameMode(string name)
    {
        Name = name;
        Initialize();
    }

    private const string SPAWN_INI_OPTIONS_SECTION = "ForcedSpawnIniOptions";

    private string mapCodeININame;

    /// <summary>
    /// Gets or sets the internal (INI) name of the game mode.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the user-interface name of the game mode.
    /// </summary>
    public string UIName { get; private set; }

    /// <summary>
    /// Gets a value indicating whether if set, this game mode cannot be played on Skirmish.
    /// </summary>
    public bool MultiplayerOnly { get; private set; }

    /// <summary>
    /// Gets a value indicating whether if set, this game mode cannot be played with AI players.
    /// </summary>
    public bool HumanPlayersOnly { get; private set; }

    /// <summary>
    /// Gets a value indicating whether if set, players are forced to random starting locations on this game mode.
    /// </summary>
    public bool ForceRandomStartLocations { get; private set; }

    /// <summary>
    /// Gets a value indicating whether if set, players are forced to different teams on this game mode.
    /// </summary>
    public bool ForceNoTeams { get; private set; }

    // </summary>
    // Override for minimum amount of players needed to play any map in this game mode.
    // </summary>
    public int MinPlayersOverride { get; private set; } = -1;

    private string forcedOptionsSection;

    public List<KeyValuePair<string, bool>> ForcedCheckBoxValues = new();
    public List<KeyValuePair<string, int>> ForcedDropDownValues = new();

    private readonly List<KeyValuePair<string, string>> forcedSpawnIniOptions = new();

    public int CoopDifficultyLevel { get; set; }

    public void Initialize()
    {
        IniFile forcedOptionsIni = new(ProgramConstants.GamePath + ClientConfiguration.Instance.MPMapsIniPath);

        CoopDifficultyLevel = forcedOptionsIni.GetIntValue(Name, "CoopDifficultyLevel", 0);
        UIName = forcedOptionsIni.GetStringValue(Name, "UIName", Name);
        MultiplayerOnly = forcedOptionsIni.GetBooleanValue(Name, "MultiplayerOnly", false);
        HumanPlayersOnly = forcedOptionsIni.GetBooleanValue(Name, "HumanPlayersOnly", false);
        ForceRandomStartLocations = forcedOptionsIni.GetBooleanValue(Name, "ForceRandomStartLocations", false);
        ForceNoTeams = forcedOptionsIni.GetBooleanValue(Name, "ForceNoTeams", false);
        MinPlayersOverride = forcedOptionsIni.GetIntValue(Name, "MinPlayersOverride", -1);
        forcedOptionsSection = forcedOptionsIni.GetStringValue(Name, "ForcedOptions", string.Empty);
        mapCodeININame = forcedOptionsIni.GetStringValue(Name, "MapCodeININame", Name + ".ini");

        string[] disallowedSides = forcedOptionsIni
            .GetStringValue(Name, "DisallowedPlayerSides", string.Empty)
            .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string sideIndex in disallowedSides)
            DisallowedPlayerSides.Add(int.Parse(sideIndex));

        ParseForcedOptions(forcedOptionsIni);

        ParseSpawnIniOptions(forcedOptionsIni);
    }

    public void ApplySpawnIniCode(IniFile spawnIni)
    {
        foreach (KeyValuePair<string, string> key in forcedSpawnIniOptions)
            spawnIni.SetStringValue("Settings", key.Key, key.Value);
    }

    private void ParseForcedOptions(IniFile forcedOptionsIni)
    {
        if (string.IsNullOrEmpty(forcedOptionsSection))
            return;

        List<string> keys = forcedOptionsIni.GetSectionKeys(forcedOptionsSection);

        if (keys == null)
            return;

        foreach (string key in keys)
        {
            string value = forcedOptionsIni.GetStringValue(forcedOptionsSection, key, string.Empty);

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

    private void ParseSpawnIniOptions(IniFile forcedOptionsIni)
    {
        string section = forcedOptionsIni.GetStringValue(Name, "ForcedSpawnIniOptions", Name + SPAWN_INI_OPTIONS_SECTION);

        List<string> spawnIniKeys = forcedOptionsIni.GetSectionKeys(section);

        if (spawnIniKeys == null)
            return;

        foreach (string key in spawnIniKeys)
        {
            forcedSpawnIniOptions.Add(new KeyValuePair<string, string>(
                key,
                forcedOptionsIni.GetStringValue(section, key, string.Empty)));
        }
    }

    public IniFile GetMapRulesIniFile()
    {
        return new IniFile(ProgramConstants.GamePath + BASE_INI_PATH + mapCodeININame);
    }

    public override int GetHashCode() => Name != null ? Name.GetHashCode() : 0;

    protected bool Equals(GameMode other) => string.Equals(Name, other?.Name, StringComparison.OrdinalIgnoreCase);
}