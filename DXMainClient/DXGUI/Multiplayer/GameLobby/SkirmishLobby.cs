using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Generic;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class SkirmishLobby : GameLobbyBase, ISwitchable
{
    private const string SETTINGS_PATH = "Client/SkirmishSettings.ini";

    private readonly TopBar topBar;

    public SkirmishLobby(WindowManager windowManager, TopBar topBar, MapLoader mapLoader, DiscordHandler discordHandler)
        : base(windowManager, "SkirmishLobby", mapLoader, false, discordHandler)
    {
        this.topBar = topBar;
    }

    public event EventHandler Exited;

    public override void Initialize()
    {
        base.Initialize();

        RandomSeed = new Random().Next();

        //InitPlayerOptionDropdowns(128, 98, 90, 48, 55, new Point(6, 24));
        InitPlayerOptionDropdowns();

        btnLeaveGame.Text = "Main Menu".L10N("UI:Main:MainMenu");

        //MapPreviewBox.EnableContextMenu = true;
        ddPlayerSides[0].AddItem("Spectator".L10N("UI:Main:SpectatorSide"), AssetLoader.LoadTexture("spectatoricon.png"));

        mapPreviewBox.LocalStartingLocationSelected += MapPreviewBox_LocalStartingLocationSelected;
        mapPreviewBox.StartingLocationApplied += MapPreviewBox_StartingLocationApplied;

        WindowManager.CenterControlOnScreen(this);

        LoadSettings();

        CheckDisallowedSides();

        CopyPlayerDataToUI();

        ProgramConstants.PlayerNameChanged += ProgramConstants_PlayerNameChanged;
        ddPlayerSides[0].SelectedIndexChanged += PlayerSideChanged;

        playerExtraOptionsPanel?.SetIsHost(true);
    }

    public void Open()
    {
        topBar.AddPrimarySwitchable(this);
        Enable();
    }

    protected override void ToggleFavoriteMap()
    {
        base.ToggleFavoriteMap();

        if (GameModeMap.IsFavorite)
            return;

        RefreshForFavoriteMapRemoved();
    }

    protected override void AddNotice(string message, Color color)
    {
        XNAMessageBox.Show(WindowManager, "Message".L10N("UI:Main:MessageTitle"), message);
    }

    protected override void OnEnabledChanged(object sender, EventArgs args)
    {
        base.OnEnabledChanged(sender, args);
        if (Enabled)
            UpdateDiscordPresence(true);
        else
            ResetDiscordPresence();
    }

    protected override void BtnLaunchGameLeftClick(object sender, EventArgs e)
    {
        string error = CheckGameValidity();

        if (error == null)
        {
            SaveSettings();
            StartGame();
            return;
        }

        XNAMessageBox.Show(WindowManager, "Cannot launch game".L10N("UI:Main:LaunchGameErrorTitle"), error);
    }

    private void ProgramConstants_PlayerNameChanged(object sender, EventArgs e)
    {
        players[0].Name = ProgramConstants.PLAYERNAME;
        CopyPlayerDataToUI();
    }

    private void MapPreviewBox_StartingLocationApplied(object sender, EventArgs e)
    {
        CopyPlayerDataToUI();
    }

    private void MapPreviewBox_LocalStartingLocationSelected(object sender, LocalStartingLocationEventArgs e)
    {
        players[0].StartingLocation = e.StartingLocationIndex + 1;
        CopyPlayerDataToUI();
    }

    private string CheckGameValidity()
    {
        int totalPlayerCount = players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
            + aIPlayers.Count;

        if (GameMode.MultiplayerOnly)
        {
            return string.Format(
                "{0} can only be played on CnCNet and LAN.".L10N("UI:Main:GameModeMultiplayerOnly"),
                GameMode.UIName);
        }

        if (GameMode.MinPlayersOverride > -1 && totalPlayerCount < GameMode.MinPlayersOverride)
        {
            return string.Format(
                "{0} cannot be played with less than {1} players.".L10N("UI:Main:GameModeInsufficientPlayers"),
                     GameMode.UIName, GameMode.MinPlayersOverride);
        }

        if (Map.MultiplayerOnly)
        {
            return "The selected map can only be played on CnCNet and LAN.".L10N("UI:Main:MapMultiplayerOnly");
        }

        if (totalPlayerCount < Map.MinPlayers)
        {
            return string.Format(
                "The selected map cannot be played with less than {0} players.".L10N("UI:Main:MapInsufficientPlayers"),
                Map.MinPlayers);
        }

        if (Map.EnforceMaxPlayers)
        {
            if (totalPlayerCount > Map.MaxPlayers)
            {
                return string.Format(
                    "The selected map cannot be played with more than {0} players.".L10N("UI:Main:MapTooManyPlayers"),
                    Map.MaxPlayers);
            }

            IEnumerable<PlayerInfo> concatList = players.Concat(aIPlayers);

            foreach (PlayerInfo pInfo in concatList)
            {
                if (pInfo.StartingLocation == 0)
                    continue;

                if (concatList.Count(p => p.StartingLocation == pInfo.StartingLocation) > 1)
                {
                    return "Multiple players cannot share the same starting location on the selected map.".L10N("UI:Main:StartLocationOccupied");
                }
            }
        }

        if (Map.IsCoop && players[0].SideId == ddPlayerSides[0].Items.Count - 1)
        {
            return "Co-op missions cannot be spectated. You'll have to show a bit more effort to cheat here.".L10N("UI:Main:CoOpMissionSpectatorPrompt");
        }

        string teamMappingsError = GetTeamMappingsError();
        if (!string.IsNullOrEmpty(teamMappingsError))
            return teamMappingsError;

        return null;
    }

    protected override void BtnLeaveGameLeftClick(object sender, EventArgs e)
    {
        Enabled = false;
        Visible = false;

        Exited?.Invoke(this, EventArgs.Empty);

        topBar.RemovePrimarySwitchable(this);
        ResetDiscordPresence();
    }

    protected override void UpdateDiscordPresence(bool resetTimer = false)
    {
        if (discordHandler == null || Map == null || GameMode == null || !Initialized)
            return;

        int playerIndex = players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);
        if (playerIndex is >= MAXPLAYERCOUNT or < 0)
            return;

        XNAClientDropDown sideDropDown = ddPlayerSides[playerIndex];
        if (sideDropDown.SelectedItem == null)
            return;

        string side = sideDropDown.SelectedItem.Text;
        string currentState = ProgramConstants.IsInGame ? "In Game" : "Setting Up";

        discordHandler.UpdatePresence(
            Map.Name, GameMode.Name, currentState, side, resetTimer);
    }

    private void PlayerSideChanged(object sender, EventArgs e)
    {
        UpdateDiscordPresence();
    }

    protected override bool AllowPlayerOptionsChange()
    {
        return true;
    }

    protected override int GetDefaultMapRankIndex(GameModeMap gameModeMap)
    {
        return StatisticsManager.Instance.GetSkirmishRankForDefaultMap(gameModeMap.Map.Name, gameModeMap.Map.MaxPlayers);
    }

    protected override void GameProcessExited()
    {
        base.GameProcessExited();

        DdGameModeMapFilterSelectedIndexChanged(null, EventArgs.Empty); // Refresh ranks

        RandomSeed = new Random().Next();
    }

    public void SwitchOn()
    {
        Enable();
    }

    public void SwitchOff()
    {
        Disable();
    }

    public string GetSwitchName()
    {
        return "Skirmish Lobby".L10N("UI:Main:SkirmishLobby");
    }

    protected override void UpdateMapPreviewBoxEnabledStatus()
    {
        mapPreviewBox.EnableContextMenu = !((Map != null && Map.ForceRandomStartLocations) || (GameMode != null && GameMode.ForceRandomStartLocations) || GetPlayerExtraOptions().IsForceRandomStarts);
        mapPreviewBox.EnableStartLocationSelection = mapPreviewBox.EnableContextMenu;
    }

    /// <summary>
    /// Saves skirmish settings to an INI file on the file system.
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            // Delete the file so we don't keep potential extra AI players that already exist in the file
            File.Delete(ProgramConstants.GamePath + SETTINGS_PATH);

            IniFile skirmishSettingsIni = new(ProgramConstants.GamePath + SETTINGS_PATH);

            skirmishSettingsIni.SetStringValue("Player", "Info", players[0].ToString());

            for (int i = 0; i < aIPlayers.Count; i++)
            {
                skirmishSettingsIni.SetStringValue("AIPlayers", i.ToString(), aIPlayers[i].ToString());
            }

            skirmishSettingsIni.SetStringValue("Settings", "Map", Map.SHA1);
            skirmishSettingsIni.SetStringValue("Settings", "GameModeMapFilter", ddGameModeMapFilter.SelectedItem?.Text);

            if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
            {
                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    skirmishSettingsIni.SetStringValue("GameOptions", dd.Name, dd.UserSelectedIndex + string.Empty);
                }

                foreach (GameLobbyCheckBox cb in CheckBoxes)
                {
                    skirmishSettingsIni.SetStringValue("GameOptions", cb.Name, cb.Checked.ToString());
                }
            }

            skirmishSettingsIni.WriteIniFile();
        }
        catch (Exception ex)
        {
            Logger.Log("Saving skirmish settings failed! Reason: " + ex.Message);
        }
    }

    /// <summary>
    /// Loads skirmish settings from an INI file on the file system.
    /// </summary>
    private void LoadSettings()
    {
        if (!File.Exists(ProgramConstants.GamePath + SETTINGS_PATH))
        {
            InitDefaultSettings();
            return;
        }

        IniFile skirmishSettingsIni = new(ProgramConstants.GamePath + SETTINGS_PATH);

        string gameModeMapFilterName = skirmishSettingsIni.GetStringValue("Settings", "GameModeMapFilter", string.Empty);
        if (string.IsNullOrEmpty(gameModeMapFilterName))
            gameModeMapFilterName = skirmishSettingsIni.GetStringValue("Settings", "GameMode", string.Empty); // legacy

        if (ddGameModeMapFilter.Items.Find(i => i.Text == gameModeMapFilterName)?.Tag is not GameModeMapFilter gameModeMapFilter || !gameModeMapFilter.Any())
            gameModeMapFilter = GetDefaultGameModeMapFilter();

        GameModeMap gameModeMap = gameModeMapFilter.GetGameModeMaps().First();

        if (gameModeMap != null)
        {
            GameModeMap = gameModeMap;

            ddGameModeMapFilter.SelectedIndex = ddGameModeMapFilter.Items.FindIndex(i => i.Tag == gameModeMapFilter);

            string mapSHA1 = skirmishSettingsIni.GetStringValue("Settings", "Map", string.Empty);

            int gameModeMapIndex = gameModeMapFilter.GetGameModeMaps().FindIndex(gmm => gmm.Map.SHA1 == mapSHA1);

            if (gameModeMapIndex > -1)
            {
                lbGameModeMapList.SelectedIndex = gameModeMapIndex;

                while (gameModeMapIndex > lbGameModeMapList.LastIndex)
                    lbGameModeMapList.TopIndex++;
            }
        }
        else
        {
            LoadDefaultGameModeMap();
        }

        PlayerInfo player = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("Player", "Info", string.Empty));

        if (player == null)
        {
            Logger.Log("Failed to load human player information from skirmish settings!");
            InitDefaultSettings();
            return;
        }

        CheckLoadedPlayerVariableBounds(player);

        player.Name = ProgramConstants.PLAYERNAME;
        players.Add(player);

        List<string> keys = skirmishSettingsIni.GetSectionKeys("AIPlayers");

        if (keys == null)
        {
            keys = new List<string>(); // No point skip parsing all settings if only AI info is missing.

            //Logger.Log("AI player information doesn't exist in skirmish settings!");
            //InitDefaultSettings();
            //return;
        }

        bool aIAllowed = !(Map.MultiplayerOnly || GameMode.MultiplayerOnly) || !(Map.HumanPlayersOnly || GameMode.HumanPlayersOnly);
        foreach (string key in keys)
        {
            if (!aIAllowed)
                break;
            PlayerInfo aiPlayer = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("AIPlayers", key, string.Empty));

            CheckLoadedPlayerVariableBounds(aiPlayer, true);

            if (aiPlayer == null)
            {
                Logger.Log("Failed to load AI player information from skirmish settings!");
                InitDefaultSettings();
                return;
            }

            if (aIPlayers.Count < MAXPLAYERCOUNT - 1)
                aIPlayers.Add(aiPlayer);
        }

        if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
        {
            foreach (GameLobbyDropDown dd in DropDowns)
            {
                // Maybe we should build an union of the game mode and map
                // forced options, we'd have less repetitive code that way
                if (GameMode != null)
                {
                    int gameModeMatchIndex = GameMode.ForcedDropDownValues.FindIndex(p => p.Key.Equals(dd.Name, StringComparison.Ordinal));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Dropdown '" + dd.Name + "' has forced value in gamemode - saved settings ignored.");
                        continue;
                    }
                }

                if (Map != null)
                {
                    int gameModeMatchIndex = Map.ForcedDropDownValues.FindIndex(p => p.Key.Equals(dd.Name, StringComparison.Ordinal));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Dropdown '" + dd.Name + "' has forced value in map - saved settings ignored.");
                        continue;
                    }
                }

                dd.UserSelectedIndex = skirmishSettingsIni.GetIntValue("GameOptions", dd.Name, dd.UserSelectedIndex);

                if (dd.UserSelectedIndex > -1 && dd.UserSelectedIndex < dd.Items.Count)
                    dd.SelectedIndex = dd.UserSelectedIndex;
            }

            foreach (GameLobbyCheckBox cb in CheckBoxes)
            {
                if (GameMode != null)
                {
                    int gameModeMatchIndex = GameMode.ForcedCheckBoxValues.FindIndex(p => p.Key.Equals(cb.Name, StringComparison.Ordinal));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Checkbox '" + cb.Name + "' has forced value in gamemode - saved settings ignored.");
                        continue;
                    }
                }

                if (Map != null)
                {
                    int gameModeMatchIndex = Map.ForcedCheckBoxValues.FindIndex(p => p.Key.Equals(cb.Name, StringComparison.Ordinal));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Checkbox '" + cb.Name + "' has forced value in map - saved settings ignored.");
                        continue;
                    }
                }

                cb.Checked = skirmishSettingsIni.GetBooleanValue("GameOptions", cb.Name, cb.Checked);
            }
        }
    }

    /// <summary>
    /// Checks that a player's color, team and starting location
    /// don't exceed allowed bounds.
    /// </summary>
    /// <param name="pInfo">The PlayerInfo.</param>
    private void CheckLoadedPlayerVariableBounds(PlayerInfo pInfo, bool isAIPlayer = false)
    {
        int sideCount = SideCount + RandomSelectorCount;
        if (isAIPlayer)
            sideCount--;

        if (pInfo.SideId < 0 || pInfo.SideId > sideCount)
        {
            pInfo.SideId = 0;
        }

        if (pInfo.ColorId < 0 || pInfo.ColorId > mPColors.Count)
        {
            pInfo.ColorId = 0;
        }

        if (pInfo.TeamId < 0 || pInfo.TeamId >= ddPlayerTeams[0].Items.Count ||
            !Map.IsCoop && (Map.ForceNoTeams || GameMode.ForceNoTeams))
        {
            pInfo.TeamId = 0;
        }

        if (pInfo.StartingLocation < 0 || pInfo.StartingLocation > MAXPLAYERCOUNT ||
            !Map.IsCoop && (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations))
        {
            pInfo.StartingLocation = 0;
        }
    }

    private void InitDefaultSettings()
    {
        players.Clear();
        aIPlayers.Clear();

        players.Add(new PlayerInfo(ProgramConstants.PLAYERNAME, 0, 0, 0, 0));
        PlayerInfo aiPlayer = new(ProgramConstants.AIPLAYERNAMES[0], 0, 0, 0, 0)
        {
            IsAI = true,
            AILevel = 2
        };
        aIPlayers.Add(aiPlayer);

        LoadDefaultGameModeMap();
    }
}