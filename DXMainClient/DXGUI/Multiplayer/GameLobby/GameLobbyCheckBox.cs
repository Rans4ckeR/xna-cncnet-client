using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

/// <summary>
/// A game option check box for the game lobby.
/// </summary>
public class GameLobbyCheckBox : XNAClientCheckBox
{
    private string customIniPath;

    private string disabledSpawnIniValue = "False";

    private string enabledSpawnIniValue = "True";

    private bool reversed;

    private string spawnIniOption;

    public GameLobbyCheckBox(WindowManager windowManager)
            : base(windowManager)
    {
    }

    public bool AllowChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets the side indices that this check box disallows when checked. Defaults to -1, which means none.
    /// </summary>
    public List<int> DisallowedSideIndices { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the last host-defined value for this check box.
    /// Defaults to the default value of Checked after the check-box has been initialized, but its
    /// value is only changed by user interaction.
    /// </summary>
    public bool HostChecked { get; set; }

    public bool IsMultiplayer { get; set; }

    public CheckBoxMapScoringMode MapScoringMode { get; private set; } = CheckBoxMapScoringMode.Irrelevant;

    /// <summary>
    /// Gets or sets a value indicating whether the last value that the local player gave for this
    /// check box. Defaults to the default value of Checked after the check-box has been
    /// initialized, but its value is only changed by user interaction.
    /// </summary>
    public bool UserChecked { get; set; }

    /// <summary>
    /// Applies the check-box's disallowed side index to a bool array that determines which sides
    /// are disabled.
    /// </summary>
    /// <param name="disallowedArray">An array that determines which sides are disabled.</param>
    public void ApplyDisallowedSideIndex(bool[] disallowedArray)
    {
        if (DisallowedSideIndices == null || DisallowedSideIndices.Count == 0)
            return;

        if (Checked != reversed)
        {
            for (int i = 0; i < DisallowedSideIndices.Count; i++)
            {
                int sideNotAllowed = DisallowedSideIndices[i];
                disallowedArray[sideNotAllowed] = true;
            }
        }
    }

    /// <summary>
    /// Applies the check-box's associated code to the map INI file.
    /// </summary>
    /// <param name="mapIni">The map INI file.</param>
    /// <param name="gameMode">Currently selected gamemode, if set.</param>
    public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
    {
        if (Checked == reversed || string.IsNullOrEmpty(customIniPath))
            return;

        MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
    }

    /// <summary>
    /// Applies the check-box's associated code to the spawn INI file.
    /// </summary>
    /// <param name="spawnIni">The spawn INI file.</param>
    public void ApplySpawnINICode(IniFile spawnIni)
    {
        if (string.IsNullOrEmpty(spawnIniOption))
            return;

        string value = disabledSpawnIniValue;
        if (Checked != reversed)
        {
            value = enabledSpawnIniValue;
        }

        spawnIni.SetStringValue("Settings", spawnIniOption, value);
    }

    public override void Initialize()
    {
        // Find the game lobby that this control belongs to and register ourselves as a game option.
        XNAControl parent = Parent;
        while (true)
        {
            if (parent == null)
                break;

            // oh no, we have a circular class reference here!
            if (parent is GameLobbyBase gameLobby)
            {
                gameLobby.CheckBoxes.Add(this);
                break;
            }

            parent = parent.Parent;
        }

        base.Initialize();
    }

    public override void OnLeftClick()
    {
        if (!AllowChanges)
            return;

        base.OnLeftClick();
        UserChecked = Checked;
    }

    public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "SpawnIniOption":
                spawnIniOption = value;
                return;

            case "EnabledSpawnIniValue":
                enabledSpawnIniValue = value;
                return;

            case "DisabledSpawnIniValue":
                disabledSpawnIniValue = value;
                return;

            case "CustomIniPath":
                customIniPath = value;
                return;

            case "Reversed":
                reversed = Conversions.BooleanFromString(value, false);
                return;

            case "CheckedMP":
                if (IsMultiplayer)
                    Checked = Conversions.BooleanFromString(value, false);
                return;

            case "Checked":
                bool checkedValue = Conversions.BooleanFromString(value, false);
                Checked = checkedValue;
                HostChecked = checkedValue;
                UserChecked = checkedValue;
                return;

            case "DisallowedSideIndex":
            case "DisallowedSideIndices":
                List<int> sides = value.Split(',').ToList()
                    .Select(s => Conversions.IntFromString(s, -1)).Distinct().ToList();
                DisallowedSideIndices.AddRange(sides.Where(s => !DisallowedSideIndices.Contains(s)));
                return;

            case "MapScoringMode":
                MapScoringMode = (CheckBoxMapScoringMode)Enum.Parse(typeof(CheckBoxMapScoringMode), value);
                return;
        }

        base.ParseAttributeFromINI(iniFile, key, value);
    }
}