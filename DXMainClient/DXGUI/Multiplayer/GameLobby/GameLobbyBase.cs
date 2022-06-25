using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientCore.Enums;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.Online.EventArguments;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

/// <summary>
/// A generic base for all game lobbies (Skirmish, LAN and CnCNet). Contains the common logic for
/// parsing game options and handling player info.
/// </summary>
public abstract class GameLobbyBase : INItializableWindow
{
    protected const int MAXPLAYERCOUNT = 8;

    protected const int PLAYEROPTIONCAPTIONY = 6;

    protected const int PLAYEROPTIONHORIZONTALMARGIN = 3;

    protected const int PLAYEROPTIONVERTICALMARGIN = 12;

    private const int DROP_DOWN_HEIGHT = 21;

    private const int RANK_HARD = 3;

    private const int RANK_NONE = 0;

    private readonly string _iniSectionName;

    private readonly string favoriteMapsLabel = "Favorite Maps".L10N("UI:Main:FavoriteMaps");

    private readonly bool isMultiplayer = false;

    private GameModeMap _gameModeMap;

    private bool disableGameOptionUpdateBroadcast = false;

    private LoadOrSaveGameOptionPresetWindow loadOrSaveGameOptionPresetWindow;

    private MatchStatistics matchStatistics;

    private XNAContextMenuItem toggleFavoriteItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameLobbyBase" /> class. Creates a new instance
    /// of the game lobby base.
    /// </summary>
    /// <param name="windowManager">windowManager.</param>
    /// <param name="iniName">The name of the lobby in GameOptions.ini.</param>
    /// <param name="mapLoader">mapLoader.</param>
    /// <param name="isMultiplayer">isMultiplayer.</param>
    /// <param name="discordHandler">discordHandler.</param>
    public GameLobbyBase(
        WindowManager windowManager,
        string iniName,
        MapLoader mapLoader,
        bool isMultiplayer,
        DiscordHandler discordHandler)
        : base(windowManager)
    {
        _iniSectionName = iniName;
        MapLoader = mapLoader;
        this.isMultiplayer = isMultiplayer;
        DiscordHandler = discordHandler;
    }

    public List<GameLobbyCheckBox> CheckBoxes { get; set; } = new();

    public List<GameLobbyDropDown> DropDowns { get; set; } = new();

    protected List<PlayerInfo> AIPlayers { get; set; } = new();

    protected string TextLaunchGame { get; } = "Launch Game".L10N("UI:Main:ButtonLaunchGame");

    protected string TextLaunchNotReady { get; } = "Not Ready".L10N("UI:Main:ButtonNotReady");

    protected string TextLaunchReady { get; } = "I'm Ready".L10N("UI:Main:ButtonIAmReady");

    protected GameLaunchButton BtnLaunchGame { get; set; }

    protected XNAClientButton BtnLeaveGame { get; set; }

    protected XNAClientStateButton<SortDirection> BtnMapSortAlphabetically { get; set; }

    protected XNAClientButton BtnPickRandomMap { get; set; }

    protected XNAClientButton BtnPlayerExtraOptionsOpen { get; set; }

    protected XNAClientButton BtnSaveLoadGameOptions { get; set; }

    protected XNAClientDropDown DdGameModeMapFilter { get; set; }

    protected XNAClientDropDown[] DdPlayerColors { get; set; }

    protected XNAClientDropDown[] DdPlayerNames { get; set; }

    protected XNAClientDropDown[] DdPlayerSides { get; set; }

    protected XNAClientDropDown[] DdPlayerStarts { get; set; }

    protected XNAClientDropDown[] DdPlayerTeams { get; set; }

    protected DiscordHandler DiscordHandler { get; set; }

    protected GameMode GameMode => GameModeMap?.GameMode;

    /// <summary>
    /// Gets or sets the currently selected game mode.
    /// </summary>
    protected GameModeMap GameModeMap
    {
        get => _gameModeMap;
        set
        {
            GameModeMap oldGameModeMap = _gameModeMap;
            _gameModeMap = value;
            if (value != null && oldGameModeMap != value)
                UpdateDiscordPresence();
        }
    }

    protected GameModeMapFilter GameModeMapFilter { get; set; }

    /// <summary>
    /// Gets the list of multiplayer game mode maps. Each is an instance of a map for a specific
    /// game mode.
    /// </summary>
    protected GameModeMapCollection GameModeMaps => MapLoader.GameModeMaps;

    protected IniFile GameOptionsIni { get; private set; }

    protected XNAMultiColumnListBox LbGameModeMapList { get; set; }

    protected XNALabel LblGameMode { get; set; }

    protected XNALabel LblGameModeSelect { get; set; }

    protected XNALabel LblMapAuthor { get; set; }

    protected XNALabel LblMapName { get; set; }

    protected XNALabel LblMapSize { get; set; }

    protected Map Map => GameModeMap?.Map;

    protected XNAContextMenu MapContextMenu { get; set; }

    protected MapLoader MapLoader { get; set; }

    protected MapPreviewBox MapPreviewBox { get; set; }

    protected List<MultiplayerColor> MPColors { get; set; }

    protected EventHandler<MultiplayerNameRightClickedEventArgs> MultiplayerNameRightClicked { get; set; }

    protected PlayerExtraOptionsPanel PlayerExtraOptionsPanel { get; set; }

    protected XNAPanel PlayerOptionsPanel { get; set; }

    protected List<PlayerInfo> Players { get; set; } = new();

    protected bool PlayerUpdatingInProgress { get; set; }

    /// <summary>
    /// Gets or sets the seed used for randomizing player options.
    /// </summary>
    protected int RandomSeed { get; set; }

    protected int RandomSelectorCount { get; private set; } = 1;

    protected List<int[]> RandomSelectors { get; set; } = new();

    protected Texture2D[] RankTextures { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether if set, the client will remove all starting
    /// waypoints from the map before launching it.
    /// </summary>
    protected bool RemoveStartingLocations { get; set; } = false;

    protected int SideCount { get; private set; }

    protected XNASuggestionTextBox TbMapSearch { get; set; }

    /// <summary>
    /// Gets or sets an unique identifier for this game.
    /// </summary>
    protected int UniqueGameID { get; set; }

    private XNAContextMenu LoadSaveGameOptionsMenu { get; set; }

    public override void Initialize()
    {
        Name = _iniSectionName;

        //if (WindowManager.RenderResolutionY < 800)
        //    ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);
        //else
        ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX - 60, WindowManager.RenderResolutionY - 32);
        WindowManager.CenterControlOnScreen(this);
        BackgroundTexture = AssetLoader.LoadTexture("gamelobbybg.png");

        RankTextures = new Texture2D[4]
        {
            AssetLoader.LoadTexture("rankNone.png"),
            AssetLoader.LoadTexture("rankEasy.png"),
            AssetLoader.LoadTexture("rankNormal.png"),
            AssetLoader.LoadTexture("rankHard.png")
        };

        MPColors = MultiplayerColor.LoadColors();

        GameOptionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + "GameOptions.ini");

        base.Initialize();

        PlayerOptionsPanel = FindChild<XNAPanel>(nameof(PlayerOptionsPanel));

        BtnLeaveGame = FindChild<XNAClientButton>(nameof(BtnLeaveGame));
        BtnLeaveGame.LeftClick += BtnLeaveGameLeftClick;

        BtnLaunchGame = FindChild<GameLaunchButton>(nameof(BtnLaunchGame));
        BtnLaunchGame.LeftClick += BtnLaunchGameLeftClick;
        BtnLaunchGame.InitStarDisplay(RankTextures);

        MapPreviewBox = FindChild<MapPreviewBox>("MapPreviewBox");
        MapPreviewBox.SetFields(Players, AIPlayers, MPColors, GameOptionsIni.GetStringValue("General", "Sides", string.Empty).Split(','), GameOptionsIni);
        MapPreviewBox.ToggleFavorite += MapPreviewBox_ToggleFavorite;

        LblMapName = FindChild<XNALabel>(nameof(LblMapName));
        LblMapAuthor = FindChild<XNALabel>(nameof(LblMapAuthor));
        LblGameMode = FindChild<XNALabel>(nameof(LblGameMode));
        LblMapSize = FindChild<XNALabel>(nameof(LblMapSize));

        LbGameModeMapList = FindChild<XNAMultiColumnListBox>("lbMapList"); // lbMapList for backwards compatibility
        LbGameModeMapList.SelectedIndexChanged += LbGameModeMapList_SelectedIndexChanged;
        LbGameModeMapList.RightClick += LbGameModeMapList_RightClick;
        LbGameModeMapList.AllowKeyboardInput = true; //!isMultiplayer

        MapContextMenu = new XNAContextMenu(WindowManager);
        MapContextMenu.Name = nameof(MapContextMenu);
        MapContextMenu.Width = 100;
        MapContextMenu.AddItem("Delete Map".L10N("UI:Main:DeleteMap"), DeleteMapConfirmation, null, CanDeleteMap);
        toggleFavoriteItem = new XNAContextMenuItem
        {
            Text = "Favorite".L10N("UI:Main:Favorite"),
            SelectAction = ToggleFavoriteMap
        };
        MapContextMenu.AddItem(toggleFavoriteItem);
        AddChild(MapContextMenu);

        XNAPanel rankHeader = new(WindowManager)
        {
            BackgroundTexture = AssetLoader.LoadTexture("rank.png")
        };
        rankHeader.ClientRectangle = new Rectangle(
            0,
            0,
            rankHeader.BackgroundTexture.Width,
            19);

        XNAListBox rankListBox = new(WindowManager)
        {
            TextBorderDistance = 2
        };

        LbGameModeMapList.AddColumn(rankHeader, rankListBox);
        LbGameModeMapList.AddColumn("MAP NAME".L10N("UI:Main:MapNameHeader"), LbGameModeMapList.Width - RankTextures[1].Width - 3);

        DdGameModeMapFilter = FindChild<XNAClientDropDown>("ddGameMode"); // ddGameMode for backwards compatibility
        DdGameModeMapFilter.SelectedIndexChanged += DdGameModeMapFilterSelectedIndexChanged;

        DdGameModeMapFilter.AddItem(CreateGameFilterItem(favoriteMapsLabel, new GameModeMapFilter(GetFavoriteGameModeMaps)));
        foreach (GameMode gm in GameModeMaps.GameModes)
            DdGameModeMapFilter.AddItem(CreateGameFilterItem(gm.UIName, new GameModeMapFilter(GetGameModeMaps(gm))));

        LblGameModeSelect = FindChild<XNALabel>(nameof(LblGameModeSelect));

        InitBtnMapSort();

        TbMapSearch = FindChild<XNASuggestionTextBox>(nameof(TbMapSearch));
        TbMapSearch.InputReceived += TbMapSearch_InputReceived;

        BtnPickRandomMap = FindChild<XNAClientButton>(nameof(BtnPickRandomMap));
        BtnPickRandomMap.LeftClick += BtnPickRandomMap_LeftClick;

        CheckBoxes.ForEach(chk => chk.CheckedChanged += ChkBox_CheckedChanged);
        DropDowns.ForEach(dd => dd.SelectedIndexChanged += Dropdown_SelectedIndexChanged);

        InitializeGameOptionPresetUI();
    }

    public bool LoadGameOptionPreset(string name)
    {
        GameOptionPreset preset = GameOptionPresets.Instance.GetPreset(name);
        if (preset == null)
            return false;

        disableGameOptionUpdateBroadcast = true;

        Dictionary<string, bool> checkBoxValues = preset.GetCheckBoxValues();
        foreach (KeyValuePair<string, bool> kvp in checkBoxValues)
        {
            GameLobbyCheckBox checkBox = CheckBoxes.Find(c => c.Name == kvp.Key);
            if (checkBox != null && checkBox.AllowChanges && checkBox.AllowChecking)
                checkBox.Checked = kvp.Value;
        }

        Dictionary<string, int> dropDownValues = preset.GetDropDownValues();
        foreach (KeyValuePair<string, int> kvp in dropDownValues)
        {
            GameLobbyDropDown dropDown = DropDowns.Find(d => d.Name == kvp.Key);
            if (dropDown != null && dropDown.AllowDropDown)
                dropDown.SelectedIndex = kvp.Value;
        }

        disableGameOptionUpdateBroadcast = false;
        OnGameOptionChanged();
        return true;
    }

    protected static string AILevelToName(int aiLevel)
    {
        return ProgramConstants.GetAILevelName(aiLevel);
    }

    protected string AddGameOptionPreset(string name)
    {
        string error = GameOptionPreset.IsNameValid(name);
        if (!string.IsNullOrEmpty(error))
            return error;

        GameOptionPreset preset = new(name);
        foreach (GameLobbyCheckBox checkBox in CheckBoxes)
        {
            preset.AddCheckBoxValue(checkBox.Name, checkBox.Checked);
        }

        foreach (GameLobbyDropDown dropDown in DropDowns)
        {
            preset.AddDropDownValue(dropDown.Name, dropDown.SelectedIndex);
        }

        GameOptionPresets.Instance.AddPreset(preset);
        return null;
    }

    protected void AddNotice(string message) => AddNotice(message, Color.White);

    protected abstract void AddNotice(string message, Color color);

    protected abstract bool AllowPlayerOptionsChange();

    protected void ApplyPlayerExtraOptions(string sender, string message)
    {
        _ = sender.ToString();
        PlayerExtraOptions playerExtraOptions = PlayerExtraOptions.FromMessage(message);

        if (playerExtraOptions.IsForceRandomSides != PlayerExtraOptionsPanel.IsForcedRandomSides())
            AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomSides, "side selection".L10N("UI:Main:SideAsANoun"));

        if (playerExtraOptions.IsForceRandomColors != PlayerExtraOptionsPanel.IsForcedRandomColors())
            AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomColors, "color selection".L10N("UI:Main:ColorAsANoun"));

        if (playerExtraOptions.IsForceRandomStarts != PlayerExtraOptionsPanel.IsForcedRandomStarts())
            AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomStarts, "start selection".L10N("UI:Main:StartPositionAsANoun"));

        if (playerExtraOptions.IsForceRandomTeams != PlayerExtraOptionsPanel.IsForcedRandomTeams())
            AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomTeams, "team selection".L10N("UI:Main:TeamAsANoun"));

        if (playerExtraOptions.IsUseTeamStartMappings != PlayerExtraOptionsPanel.IsUseTeamStartMappings())
            AddPlayerExtraOptionForcedNotice(!playerExtraOptions.IsUseTeamStartMappings, "auto ally".L10N("UI:Main:AutoAllyAsANoun"));

        SetPlayerExtraOptions(playerExtraOptions);
        UpdateMapPreviewBoxEnabledStatus();
    }

    /// <summary>
    /// Override this in a derived class to ban players.
    /// </summary>
    /// <param name="playerIndex">The index of the player that should be banned.</param>
    protected virtual void BanPlayer(int playerIndex)
    {
        // Do nothing by default
    }

    protected abstract void BtnLaunchGameLeftClick(object sender, EventArgs e);

    protected abstract void BtnLeaveGameLeftClick(object sender, EventArgs e);

    protected void BtnPlayerExtraOptionsLeftClick(object sender, EventArgs e)
    {
        if (PlayerExtraOptionsPanel.Enabled)
            PlayerExtraOptionsPanel.Disable();
        else
            PlayerExtraOptionsPanel.Enable();
    }

    /// <summary>
    /// Changes the current map and game mode.
    /// </summary>
    /// <param name="gameModeMap">The new game mode map.</param>
    protected virtual void ChangeMap(GameModeMap gameModeMap)
    {
        GameModeMap = gameModeMap;

        if (GameMode == null || Map == null)
        {
            LblMapName.Text = "Map: Unknown".L10N("UI:Main:MapUnknown");
            LblMapAuthor.Text = "By Unknown Author".L10N("UI:Main:AuthorByUnknown");
            LblGameMode.Text = "Game mode: Unknown".L10N("UI:Main:GameModeUnknown");
            LblMapSize.Text = "Size: Not available".L10N("UI:Main:MapSizeUnknown");

            MapPreviewBox.GameModeMap = null;

            return;
        }

        LblMapName.Text = "Map:".L10N("UI:Main:Map") + " " + Renderer.GetSafeString(Map.Name, LblMapName.FontIndex);
        LblMapAuthor.Text = "By".L10N("UI:Main:AuthorBy") + " " + Renderer.GetSafeString(Map.Author, LblMapAuthor.FontIndex);
        LblGameMode.Text = "Game mode:".L10N("UI:Main:GameModeLabel") + " " + GameMode.UIName;
        LblMapSize.Text = "Size:".L10N("UI:Main:MapSize") + " " + Map.GetSizeString();

        disableGameOptionUpdateBroadcast = true;

        // Clear forced options
        foreach (GameLobbyDropDown ddGameOption in DropDowns)
            ddGameOption.AllowDropDown = true;

        foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            checkBox.AllowChecking = true;

        // We could either pass the CheckBoxes and DropDowns of this class to the Map and GameMode
        // instances and let them apply their forced options, or we could do it in this class with
        // helper functions. The second approach is probably clearer.

        // We use these temp lists to determine which options WERE NOT forced by the map. We then
        // return these to user-defined settings. This prevents forced options from one map getting
        // carried to other maps.
        List<GameLobbyCheckBox> checkBoxListClone = new(CheckBoxes);
        List<GameLobbyDropDown> dropDownListClone = new(DropDowns);

        ApplyForcedCheckBoxOptions(checkBoxListClone, GameMode.ForcedCheckBoxValues);
        ApplyForcedCheckBoxOptions(checkBoxListClone, Map.ForcedCheckBoxValues);

        ApplyForcedDropDownOptions(dropDownListClone, GameMode.ForcedDropDownValues);
        ApplyForcedDropDownOptions(dropDownListClone, Map.ForcedDropDownValues);

        foreach (GameLobbyCheckBox chkBox in checkBoxListClone)
            chkBox.Checked = chkBox.HostChecked;

        foreach (GameLobbyDropDown dd in dropDownListClone)
            dd.SelectedIndex = dd.HostSelectedIndex;

        // Enable all sides by default
        foreach (XNAClientDropDown ddSide in DdPlayerSides)
        {
            ddSide.Items.ForEach(item => item.Selectable = true);
        }

        // Enable all colors by default
        foreach (XNAClientDropDown ddColor in DdPlayerColors)
        {
            ddColor.Items.ForEach(item => item.Selectable = true);
        }

        // Apply starting locations
        foreach (XNAClientDropDown ddStart in DdPlayerStarts)
        {
            ddStart.Items.Clear();

            ddStart.AddItem("???");

            for (int i = 1; i <= Map.MaxPlayers; i++)
                ddStart.AddItem(i.ToString());
        }

        // Check if AI players allowed
        bool aIAllowed = !(Map.MultiplayerOnly || GameMode.MultiplayerOnly) ||
                         !(Map.HumanPlayersOnly || GameMode.HumanPlayersOnly);
        foreach (XNAClientDropDown ddName in DdPlayerNames)
        {
            if (ddName.Items.Count > 3)
            {
                ddName.Items[1].Selectable = aIAllowed;
                ddName.Items[2].Selectable = aIAllowed;
                ddName.Items[3].Selectable = aIAllowed;
            }
        }

        if (!aIAllowed)
            AIPlayers.Clear();
        IEnumerable<PlayerInfo> concatPlayerList = Players.Concat(AIPlayers).ToList();

        foreach (PlayerInfo pInfo in concatPlayerList)
        {
            if (pInfo.StartingLocation > Map.MaxPlayers ||
                (!Map.IsCoop && (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations)))
            {
                pInfo.StartingLocation = 0;
            }

            if (!Map.IsCoop && (Map.ForceNoTeams || GameMode.ForceNoTeams))
                pInfo.TeamId = 0;
        }

        CheckDisallowedSides();

        if (Map.CoopInfo != null)
        {
            // Co-Op map disallowed color logic
            foreach (int disallowedColorIndex in Map.CoopInfo.DisallowedPlayerColors)
            {
                if (disallowedColorIndex >= MPColors.Count)
                    continue;

                foreach (XNADropDown ddColor in DdPlayerColors)
                    ddColor.Items[disallowedColorIndex + 1].Selectable = false;

                foreach (PlayerInfo pInfo in concatPlayerList)
                {
                    if (pInfo.ColorId == disallowedColorIndex + 1)
                        pInfo.ColorId = 0;
                }
            }

            // Force teams
            foreach (PlayerInfo pInfo in concatPlayerList)
                pInfo.TeamId = 1;
        }

        OnGameOptionChanged();

        MapPreviewBox.GameModeMap = GameModeMap;
        CopyPlayerDataToUI();

        disableGameOptionUpdateBroadcast = false;

        PlayerExtraOptionsPanel?.UpdateForMap(Map);
    }

    /// <summary>
    /// Applies disallowed side indexes to the side option drop-downs and player options.
    /// </summary>
    protected void CheckDisallowedSides()
    {
        bool[] disallowedSideArray = GetDisallowedSides();
        int defaultSide = 0;
        int allowedSideCount = disallowedSideArray.Count(b => b == false);

        if (allowedSideCount == 1)
        {
            // Disallow Random
            for (int i = 0; i < disallowedSideArray.Length; i++)
            {
                if (!disallowedSideArray[i])
                    defaultSide = i + RandomSelectorCount;
            }

            foreach (XNADropDown dd in DdPlayerSides)
            {
                //dd.Items[0].Selectable = false;
                for (int i = 0; i < RandomSelectorCount; i++)
                    dd.Items[i].Selectable = false;
            }
        }
        else
        {
            foreach (XNADropDown dd in DdPlayerSides)
            {
                //dd.Items[0].Selectable = true;
                for (int i = 0; i < RandomSelectorCount; i++)
                    dd.Items[i].Selectable = true;
            }
        }

        IEnumerable<PlayerInfo> concatPlayerList = Players.Concat(AIPlayers);

        // Disable custom random groups if all or all except one of included sides are unavailable.
        int c = 0;
        List<PlayerInfo> playerInfos = concatPlayerList.ToList();
        foreach (int[] randomSides in RandomSelectors)
        {
            int disableCount = 0;

            foreach (int side in randomSides)
            {
                if (disallowedSideArray[side])
                    disableCount++;
            }

            bool disabled = disableCount >= randomSides.Length - 1;

            foreach (XNADropDown dd in DdPlayerSides)
                dd.Items[1 + c].Selectable = !disabled;

            foreach (PlayerInfo pInfo in playerInfos)
            {
                if (pInfo.SideId == 1 + c && disabled)
                    pInfo.SideId = defaultSide;
            }

            c++;
        }

        // Go over the side array and either disable or enable the side dropdown options depending
        // on whether the side is available
        for (int i = 0; i < disallowedSideArray.Length; i++)
        {
            bool disabled = disallowedSideArray[i];

            if (disabled)
            {
                foreach (XNADropDown dd in DdPlayerSides)
                    dd.Items[i + RandomSelectorCount].Selectable = false;

                // Change the sides of players that use the disabled side to the default side
                foreach (PlayerInfo pInfo in playerInfos)
                {
                    if (pInfo.SideId == i + RandomSelectorCount)
                        pInfo.SideId = defaultSide;
                }
            }
            else
            {
                foreach (XNADropDown dd in DdPlayerSides)
                    dd.Items[i + RandomSelectorCount].Selectable = true;
            }
        }

        // If only 1 side is allowed, change all players' sides to that
        if (allowedSideCount == 1)
        {
            foreach (PlayerInfo pInfo in playerInfos)
            {
                if (pInfo.SideId == 0)
                    pInfo.SideId = defaultSide;
            }
        }

        if (Map != null && Map.CoopInfo != null)
        {
            // Disallow spectator
            foreach (PlayerInfo pInfo in playerInfos)
            {
                if (pInfo.SideId == GetSpectatorSideIndex())
                    pInfo.SideId = defaultSide;
            }

            foreach (XNADropDown dd in DdPlayerSides)
            {
                if (dd.Items.Count > GetSpectatorSideIndex())
                    dd.Items[SideCount + RandomSelectorCount].Selectable = false;
            }
        }
        else
        {
            foreach (XNADropDown dd in DdPlayerSides)
            {
                if (dd.Items.Count > SideCount + RandomSelectorCount)
                    dd.Items[SideCount + RandomSelectorCount].Selectable = true;
            }
        }
    }

    /// <summary>
    /// Sets the ready status of all non-host human players to false.
    /// </summary>
    /// <param name="resetAutoReady">If set, players with autoready enabled are reset as well.</param>
    protected void ClearReadyStatuses(bool resetAutoReady = false)
    {
        for (int i = 1; i < Players.Count; i++)
        {
            if (resetAutoReady || !Players[i].AutoReady || Players[i].IsInGame)
                Players[i].Ready = false;
        }
    }

    /// <summary>
    /// "Copies" player information from the UI to internal memory, applying users' player options changes.
    /// </summary>
    /// <param name="sender">sender.</param>
    /// <param name="e">event args.</param>
    protected virtual void CopyPlayerDataFromUI(object sender, EventArgs e)
    {
        if (PlayerUpdatingInProgress)
            return;

        XNADropDown senderDropDown = (XNADropDown)sender;
        if ((bool)senderDropDown.Tag)
            ClearReadyStatuses();

        int? oldSideId = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId;

        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];

            pInfo.ColorId = DdPlayerColors[pId].SelectedIndex;
            pInfo.SideId = DdPlayerSides[pId].SelectedIndex;
            pInfo.StartingLocation = DdPlayerStarts[pId].SelectedIndex;
            pInfo.TeamId = DdPlayerTeams[pId].SelectedIndex;

            if (pInfo.SideId == SideCount + RandomSelectorCount)
                pInfo.StartingLocation = 0;

            XNADropDown ddName = DdPlayerNames[pId];

            switch (ddName.SelectedIndex)
            {
                case 0:
                    break;

                case 1:
                    ddName.SelectedIndex = 0;
                    break;

                case 2:
                    KickPlayer(pId);
                    break;

                case 3:
                    BanPlayer(pId);
                    break;
            }
        }

        AIPlayers.Clear();
        for (int cmbId = Players.Count; cmbId < 8; cmbId++)
        {
            XNADropDown dd = DdPlayerNames[cmbId];
            dd.Items[0].Text = "-";

            if (dd.SelectedIndex < 1)
                continue;

            PlayerInfo aiPlayer = new()
            {
                Name = dd.Items[dd.SelectedIndex].Text,
                AILevel = 2 - (dd.SelectedIndex - 1),
                SideId = Math.Max(DdPlayerSides[cmbId].SelectedIndex, 0),
                ColorId = Math.Max(DdPlayerColors[cmbId].SelectedIndex, 0),
                StartingLocation = Math.Max(DdPlayerStarts[cmbId].SelectedIndex, 0),
                TeamId = Map != null && Map.IsCoop ? 1 : Math.Max(DdPlayerTeams[cmbId].SelectedIndex, 0),
                IsAI = true
            };

            AIPlayers.Add(aiPlayer);
        }

        CopyPlayerDataToUI();
        BtnLaunchGame.SetRank(GetRank());

        if (oldSideId != Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId)
            UpdateDiscordPresence();
    }

    /// <summary>
    /// Applies player information changes done in memory to the UI.
    /// </summary>
    protected virtual void CopyPlayerDataToUI()
    {
        PlayerUpdatingInProgress = true;

        bool allowOptionsChange = AllowPlayerOptionsChange();
        PlayerExtraOptions playerExtraOptions = GetPlayerExtraOptions();

        // Human players
        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];

            pInfo.Index = pId;

            XNADropDown ddPlayerName = DdPlayerNames[pId];
            ddPlayerName.Items[0].Text = pInfo.Name;
            ddPlayerName.Items[1].Text = string.Empty;
            ddPlayerName.Items[2].Text = "Kick".L10N("UI:Main:Kick");
            ddPlayerName.Items[3].Text = "Ban".L10N("UI:Main:Ban");
            ddPlayerName.SelectedIndex = 0;
            ddPlayerName.AllowDropDown = false;

            bool allowPlayerOptionsChange = allowOptionsChange || pInfo.Name == ProgramConstants.PLAYERNAME;

            DdPlayerSides[pId].SelectedIndex = pInfo.SideId;
            DdPlayerSides[pId].AllowDropDown = !playerExtraOptions.IsForceRandomSides && allowPlayerOptionsChange;

            DdPlayerColors[pId].SelectedIndex = pInfo.ColorId;
            DdPlayerColors[pId].AllowDropDown = !playerExtraOptions.IsForceRandomColors && allowPlayerOptionsChange;

            DdPlayerStarts[pId].SelectedIndex = pInfo.StartingLocation;

            DdPlayerTeams[pId].SelectedIndex = pInfo.TeamId;
            if (GameModeMap != null)
            {
                DdPlayerTeams[pId].AllowDropDown = !playerExtraOptions.IsForceRandomTeams && allowPlayerOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                DdPlayerStarts[pId].AllowDropDown = !playerExtraOptions.IsForceRandomStarts && allowPlayerOptionsChange && (Map.IsCoop || (!Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations));
            }
        }

        // AI players
        for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
        {
            PlayerInfo aiInfo = AIPlayers[aiId];

            int index = Players.Count + aiId;

            aiInfo.Index = index;

            XNADropDown ddPlayerName = DdPlayerNames[index];
            ddPlayerName.Items[0].Text = "-";
            ddPlayerName.Items[1].Text = ProgramConstants.AIPLAYERNAMES[0];
            ddPlayerName.Items[2].Text = ProgramConstants.AIPLAYERNAMES[1];
            ddPlayerName.Items[3].Text = ProgramConstants.AIPLAYERNAMES[2];
            ddPlayerName.SelectedIndex = 3 - aiInfo.AILevel;
            ddPlayerName.AllowDropDown = allowOptionsChange;

            DdPlayerSides[index].SelectedIndex = aiInfo.SideId;
            DdPlayerSides[index].AllowDropDown = !playerExtraOptions.IsForceRandomSides && allowOptionsChange;

            DdPlayerColors[index].SelectedIndex = aiInfo.ColorId;
            DdPlayerColors[index].AllowDropDown = !playerExtraOptions.IsForceRandomColors && allowOptionsChange;

            DdPlayerStarts[index].SelectedIndex = aiInfo.StartingLocation;

            DdPlayerTeams[index].SelectedIndex = aiInfo.TeamId;

            if (GameModeMap != null)
            {
                DdPlayerTeams[index].AllowDropDown = !playerExtraOptions.IsForceRandomTeams && allowOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                DdPlayerStarts[index].AllowDropDown = !playerExtraOptions.IsForceRandomStarts && allowOptionsChange && (Map.IsCoop || (!Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations));
            }
        }

        // Unused player slots
        for (int ddIndex = Players.Count + AIPlayers.Count; ddIndex < MAXPLAYERCOUNT; ddIndex++)
        {
            XNADropDown ddPlayerName = DdPlayerNames[ddIndex];
            ddPlayerName.AllowDropDown = false;
            ddPlayerName.Items[0].Text = string.Empty;
            ddPlayerName.Items[1].Text = ProgramConstants.AIPLAYERNAMES[0];
            ddPlayerName.Items[2].Text = ProgramConstants.AIPLAYERNAMES[1];
            ddPlayerName.Items[3].Text = ProgramConstants.AIPLAYERNAMES[2];
            ddPlayerName.SelectedIndex = 0;

            DdPlayerSides[ddIndex].SelectedIndex = -1;
            DdPlayerSides[ddIndex].AllowDropDown = false;

            DdPlayerColors[ddIndex].SelectedIndex = -1;
            DdPlayerColors[ddIndex].AllowDropDown = false;

            DdPlayerStarts[ddIndex].SelectedIndex = -1;
            DdPlayerStarts[ddIndex].AllowDropDown = false;

            DdPlayerTeams[ddIndex].SelectedIndex = -1;
            DdPlayerTeams[ddIndex].AllowDropDown = false;
        }

        if (allowOptionsChange && Players.Count + AIPlayers.Count < MAXPLAYERCOUNT)
            DdPlayerNames[Players.Count + AIPlayers.Count].AllowDropDown = true;

        MapPreviewBox.UpdateStartingLocationTexts();
        UpdateMapPreviewBoxEnabledStatus();

        PlayerUpdatingInProgress = false;
    }

    protected void DdGameModeMapFilterSelectedIndexChanged(object sender, EventArgs e)
    {
        GameModeMapFilter = DdGameModeMapFilter.SelectedItem.Tag as GameModeMapFilter;

        TbMapSearch.Text = string.Empty;
        TbMapSearch.OnSelectedChanged();

        ListMaps();

        if (LbGameModeMapList.SelectedIndex == -1)
            LbGameModeMapList.SelectedIndex = 0; // Select default GameModeMap
        else
            ChangeMap(GameModeMap);
    }

    protected virtual PlayerInfo FindLocalPlayer() => Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

    protected virtual void GameProcessExited()
    {
        GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

        Logger.Log("GameProcessExited: Parsing statistics.");

        matchStatistics.ParseStatistics(ProgramConstants.GamePath, false);

        Logger.Log("GameProcessExited: Adding match to statistics.");

        StatisticsManager.Instance.AddMatchAndSaveDatabase(true, matchStatistics);

        ClearReadyStatuses();

        CopyPlayerDataToUI();

        UpdateDiscordPresence(true);
    }

    protected GameModeMapFilter GetDefaultGameModeMapFilter()
    {
        return DdGameModeMapFilter.Items[GetDefaultGameModeMapFilterIndex()].Tag as GameModeMapFilter;
    }

    protected int GetDefaultGameModeMapFilterIndex()
    {
        return DdGameModeMapFilter.Items.FindIndex(i => (i.Tag as GameModeMapFilter)?.Any() ?? false);
    }

    protected abstract int GetDefaultMapRankIndex(GameModeMap gameModeMap);

    /// <summary>
    /// Gets a list of side indexes that are disallowed.
    /// </summary>
    /// <returns>A list of disallowed side indexes.</returns>
    protected bool[] GetDisallowedSides()
    {
        bool[] returnValue = new bool[SideCount];

        if (Map != null && Map.CoopInfo != null)
        {
            // Co-Op map disallowed side logic
            foreach (int disallowedSideIndex in Map.CoopInfo.DisallowedPlayerSides)
                returnValue[disallowedSideIndex] = true;
        }

        if (GameMode != null)
        {
            foreach (int disallowedSideIndex in GameMode.DisallowedPlayerSides)
                returnValue[disallowedSideIndex] = true;
        }

        foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            checkBox.ApplyDisallowedSideIndex(returnValue);

        return returnValue;
    }

    protected GameType GetGameType()
    {
        int teamCount = GetPvPTeamCount();

        if (teamCount == 0)
            return GameType.FFA;

        if (teamCount == 1)
            return GameType.Coop;

        return GameType.TeamGame;
    }

    protected virtual string GetIPAddressForPlayer(PlayerInfo player) => "0.0.0.0";

    protected PlayerExtraOptions GetPlayerExtraOptions() =>
        PlayerExtraOptionsPanel == null ? new PlayerExtraOptions() : PlayerExtraOptionsPanel.GetPlayerExtraOptions();

    protected PlayerInfo GetPlayerInfoForIndex(int playerIndex)
    {
        if (playerIndex < Players.Count)
            return Players[playerIndex];

        if (playerIndex < Players.Count + AIPlayers.Count)
            return AIPlayers[playerIndex - Players.Count];

        return null;
    }

    protected int GetRank()
    {
        if (GameMode == null || Map == null)
            return RANK_NONE;

        foreach (GameLobbyCheckBox checkBox in CheckBoxes)
        {
            if ((checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && checkBox.Checked) ||
                (checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && !checkBox.Checked))
            {
                return RANK_NONE;
            }
        }

        PlayerInfo localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

        if (localPlayer == null)
            return RANK_NONE;

        if (IsPlayerSpectator(localPlayer))
            return RANK_NONE;

        // These variables are used by both the skirmish and multiplayer code paths
        int[] teamMemberCounts = new int[5];
        int lowestEnemyAILevel = 2;
        int highestAllyAILevel = 0;

        foreach (PlayerInfo aiPlayer in AIPlayers)
        {
            teamMemberCounts[aiPlayer.TeamId]++;

            if (aiPlayer.TeamId > 0 && aiPlayer.TeamId == localPlayer.TeamId)
            {
                if (aiPlayer.ReversedAILevel > highestAllyAILevel)
                    highestAllyAILevel = aiPlayer.ReversedAILevel;
            }
            else
            {
                if (aiPlayer.ReversedAILevel < lowestEnemyAILevel)
                    lowestEnemyAILevel = aiPlayer.ReversedAILevel;
            }
        }

        if (isMultiplayer)
        {
            if (Players.Count == 1)
                return RANK_NONE;

            // PvP stars for 2-player and 3-player maps
            if (Map.MaxPlayers <= 3)
            {
                List<PlayerInfo> filteredPlayers = Players.Where(p => !IsPlayerSpectator(p)).ToList();

                if (AIPlayers.Count > 0)
                    return RANK_NONE;

                if (filteredPlayers.Count != Map.MaxPlayers)
                    return RANK_NONE;

                int localTeamIndex = localPlayer.TeamId;
                if (localTeamIndex > 0 && filteredPlayers.Count(p => p.TeamId == localTeamIndex) > 1)
                    return RANK_NONE;

                return RANK_HARD;
            }

            // Coop stars for maps with 4 or more players See the code in
            // StatisticsManager.GetRankForCoopMatch for the conditions
            if (Players.Find(IsPlayerSpectator) != null)
                return RANK_NONE;

            if (AIPlayers.Count == 0)
                return RANK_NONE;

            if (Players.Find(p => p.TeamId != localPlayer.TeamId) != null)
                return RANK_NONE;

            if (Players.Find(p => p.TeamId == 0) != null)
                return RANK_NONE;

            if (AIPlayers.Find(p => p.TeamId == 0) != null)
                return RANK_NONE;

            teamMemberCounts[localPlayer.TeamId] += Players.Count;

            if (lowestEnemyAILevel < highestAllyAILevel)
            {
                // Check that the player's AI allies aren't stronger
                return RANK_NONE;
            }

            // Check that all teams have at least as many players as the human players' team
            int allyCount = teamMemberCounts[localPlayer.TeamId];

            for (int i = 1; i < 5; i++)
            {
                if (i == localPlayer.TeamId)
                    continue;

                if (teamMemberCounts[i] > 0)
                {
                    if (teamMemberCounts[i] < allyCount)
                        return RANK_NONE;
                }
            }

            return lowestEnemyAILevel + 1;
        }

        // ********* Skirmish! *********
        if (AIPlayers.Count != Map.MaxPlayers - 1)
            return RANK_NONE;

        teamMemberCounts[localPlayer.TeamId]++;

        if (lowestEnemyAILevel < highestAllyAILevel)
        {
            // Check that the player's AI allies aren't stronger
            return RANK_NONE;
        }

        if (localPlayer.TeamId > 0)
        {
            // Check that all teams have at least as many players as the local player's team
            int allyCount = teamMemberCounts[localPlayer.TeamId];

            for (int i = 1; i < 5; i++)
            {
                if (i == localPlayer.TeamId)
                    continue;

                if (teamMemberCounts[i] > 0)
                {
                    if (teamMemberCounts[i] < allyCount)
                        return RANK_NONE;
                }
            }

            // Check that there is a team other than the players' team that is at least as large
            bool pass = false;
            for (int i = 1; i < 5; i++)
            {
                if (i == localPlayer.TeamId)
                    continue;

                if (teamMemberCounts[i] >= allyCount)
                {
                    pass = true;
                    break;
                }
            }

            if (!pass)
                return RANK_NONE;
        }

        return lowestEnemyAILevel + 1;
    }

    protected string GetTeamMappingsError() => GetPlayerExtraOptions()?.GetTeamMappingsError();

    protected void HandleGameOptionPresetLoadCommand(GameOptionPresetEventArgs e) => HandleGameOptionPresetLoadCommand(e.PresetName);

    protected void HandleGameOptionPresetLoadCommand(string presetName)
    {
        if (LoadGameOptionPreset(presetName))
            AddNotice("Game option preset loaded succesfully.".L10N("UI:Main:PresetLoaded"));
        else
            AddNotice(string.Format("Preset {0} not found!".L10N("UI:Main:PresetNotFound"), presetName));
    }

    protected void HandleGameOptionPresetSaveCommand(GameOptionPresetEventArgs e) => HandleGameOptionPresetSaveCommand(e.PresetName);

    protected void HandleGameOptionPresetSaveCommand(string presetName)
    {
        string error = AddGameOptionPreset(presetName);
        if (!string.IsNullOrEmpty(error))
            AddNotice(error);
    }

    /// <summary>
    /// Initializes the player option drop-down controls.
    /// </summary>
    protected void InitPlayerOptionDropdowns()
    {
        DdPlayerNames = new XNAClientDropDown[MAXPLAYERCOUNT];
        DdPlayerSides = new XNAClientDropDown[MAXPLAYERCOUNT];
        DdPlayerColors = new XNAClientDropDown[MAXPLAYERCOUNT];
        DdPlayerStarts = new XNAClientDropDown[MAXPLAYERCOUNT];
        DdPlayerTeams = new XNAClientDropDown[MAXPLAYERCOUNT];

        int playerOptionVecticalMargin = ConfigIni.GetIntValue(Name, "PlayerOptionVerticalMargin", PLAYEROPTIONVERTICALMARGIN);
        int playerOptionHorizontalMargin = ConfigIni.GetIntValue(Name, "PlayerOptionHorizontalMargin", PLAYEROPTIONHORIZONTALMARGIN);
        int playerOptionCaptionLocationY = ConfigIni.GetIntValue(Name, "PlayerOptionCaptionLocationY", PLAYEROPTIONCAPTIONY);
        int playerNameWidth = ConfigIni.GetIntValue(Name, "PlayerNameWidth", 136);
        int sideWidth = ConfigIni.GetIntValue(Name, "SideWidth", 91);
        int colorWidth = ConfigIni.GetIntValue(Name, "ColorWidth", 79);
        int startWidth = ConfigIni.GetIntValue(Name, "StartWidth", 49);
        int teamWidth = ConfigIni.GetIntValue(Name, "TeamWidth", 46);
        int locationX = ConfigIni.GetIntValue(Name, "PlayerOptionLocationX", 25);
        int locationY = ConfigIni.GetIntValue(Name, "PlayerOptionLocationY", 24);

        // InitPlayerOptionDropdowns(136, 91, 79, 49, 46, new Point(25, 24));
        string[] sides = ClientConfiguration.Instance.Sides.Split(',');
        SideCount = sides.Length;

        List<string> selectorNames = new();
        GetRandomSelectors(selectorNames, RandomSelectors);
        RandomSelectorCount = RandomSelectors.Count + 1;
        MapPreviewBox.RandomSelectorCount = RandomSelectorCount;

        string randomColor = GameOptionsIni.GetStringValue("General", "RandomColor", "255,255,255");

        for (int i = MAXPLAYERCOUNT - 1; i > -1; i--)
        {
            XNAClientDropDown ddPlayerName = new(WindowManager)
            {
                Name = "ddPlayerName" + i,
                ClientRectangle = new Rectangle(
                    locationX,
                    locationY + ((DROP_DOWN_HEIGHT + playerOptionVecticalMargin) * i),
                    playerNameWidth,
                    DROP_DOWN_HEIGHT)
            };
            ddPlayerName.AddItem(string.Empty);
            ProgramConstants.AIPLAYERNAMES.ForEach(ddPlayerName.AddItem);
            ddPlayerName.AllowDropDown = true;
            ddPlayerName.SelectedIndexChanged += CopyPlayerDataFromUI;
            ddPlayerName.RightClick += MultiplayerName_RightClick;
            ddPlayerName.Tag = true;

            XNAClientDropDown ddPlayerSide = new(WindowManager)
            {
                Name = "ddPlayerSide" + i,
                ClientRectangle = new Rectangle(
                    ddPlayerName.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y,
                    sideWidth,
                    DROP_DOWN_HEIGHT)
            };
            ddPlayerSide.AddItem("Random".L10N("UI:Main:RandomSide"), LoadTextureOrNull("randomicon.png"));
            foreach (string randomSelector in selectorNames)
                ddPlayerSide.AddItem(randomSelector, LoadTextureOrNull(randomSelector + "icon.png"));
            foreach (string sideName in sides)
                ddPlayerSide.AddItem(sideName, LoadTextureOrNull(sideName + "icon.png"));
            ddPlayerSide.AllowDropDown = false;
            ddPlayerSide.SelectedIndexChanged += CopyPlayerDataFromUI;
            ddPlayerSide.Tag = true;

            XNAClientDropDown ddPlayerColor = new(WindowManager)
            {
                Name = "ddPlayerColor" + i,
                ClientRectangle = new Rectangle(
                    ddPlayerSide.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y,
                    colorWidth,
                    DROP_DOWN_HEIGHT)
            };
            ddPlayerColor.AddItem("Random".L10N("UI:Main:RandomColor"), AssetLoader.GetColorFromString(randomColor));
            foreach (MultiplayerColor mpColor in MPColors)
                ddPlayerColor.AddItem(mpColor.Name, mpColor.XnaColor);
            ddPlayerColor.AllowDropDown = false;
            ddPlayerColor.SelectedIndexChanged += CopyPlayerDataFromUI;
            ddPlayerColor.Tag = false;

            XNAClientDropDown ddPlayerTeam = new(WindowManager)
            {
                Name = "ddPlayerTeam" + i,
                ClientRectangle = new Rectangle(
                    ddPlayerColor.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y,
                    teamWidth,
                    DROP_DOWN_HEIGHT)
            };
            ddPlayerTeam.AddItem("-");
            ProgramConstants.TEAMS.ForEach(ddPlayerTeam.AddItem);
            ddPlayerTeam.AllowDropDown = false;
            ddPlayerTeam.SelectedIndexChanged += CopyPlayerDataFromUI;
            ddPlayerTeam.Tag = true;

            XNAClientDropDown ddPlayerStart = new(WindowManager)
            {
                Name = "ddPlayerStart" + i,
                ClientRectangle = new Rectangle(
                    ddPlayerTeam.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y,
                    startWidth,
                    DROP_DOWN_HEIGHT)
            };
            for (int j = 1; j < 9; j++)
                ddPlayerStart.AddItem(j.ToString());
            ddPlayerStart.AllowDropDown = false;
            ddPlayerStart.SelectedIndexChanged += CopyPlayerDataFromUI;
            ddPlayerStart.Visible = false;
            ddPlayerStart.Enabled = false;
            ddPlayerStart.Tag = true;

            DdPlayerNames[i] = ddPlayerName;
            DdPlayerSides[i] = ddPlayerSide;
            DdPlayerColors[i] = ddPlayerColor;
            DdPlayerStarts[i] = ddPlayerStart;
            DdPlayerTeams[i] = ddPlayerTeam;

            PlayerOptionsPanel.AddChild(ddPlayerName);
            PlayerOptionsPanel.AddChild(ddPlayerSide);
            PlayerOptionsPanel.AddChild(ddPlayerColor);
            PlayerOptionsPanel.AddChild(ddPlayerStart);
            PlayerOptionsPanel.AddChild(ddPlayerTeam);

            ReadINIForControl(ddPlayerName);
            ReadINIForControl(ddPlayerSide);
            ReadINIForControl(ddPlayerColor);
            ReadINIForControl(ddPlayerStart);
            ReadINIForControl(ddPlayerTeam);
        }

        XNALabel lblName = GeneratePlayerOptionCaption("lblName", "PLAYER".L10N("UI:Main:PlayerOptionPlayer"), DdPlayerNames[0].X, playerOptionCaptionLocationY);
        XNALabel lblSide = GeneratePlayerOptionCaption("lblSide", "SIDE".L10N("UI:Main:PlayerOptionSide"), DdPlayerSides[0].X, playerOptionCaptionLocationY);
        XNALabel lblColor = GeneratePlayerOptionCaption("lblColor", "COLOR".L10N("UI:Main:PlayerOptionColor"), DdPlayerColors[0].X, playerOptionCaptionLocationY);

        XNALabel lblStart = GeneratePlayerOptionCaption("lblStart", "START".L10N("UI:Main:PlayerOptionStart"), DdPlayerStarts[0].X, playerOptionCaptionLocationY);
        lblStart.Visible = false;

        XNALabel lblTeam = GeneratePlayerOptionCaption("lblTeam", "TEAM".L10N("UI:Main:PlayerOptionTeam"), DdPlayerTeams[0].X, playerOptionCaptionLocationY);

        ReadINIForControl(lblName);
        ReadINIForControl(lblSide);
        ReadINIForControl(lblColor);
        ReadINIForControl(lblStart);
        ReadINIForControl(lblTeam);

        BtnPlayerExtraOptionsOpen = FindChild<XNAClientButton>(nameof(BtnPlayerExtraOptionsOpen), true);
        if (BtnPlayerExtraOptionsOpen != null)
        {
            PlayerExtraOptionsPanel = FindChild<PlayerExtraOptionsPanel>(nameof(PlayerExtraOptionsPanel));
            PlayerExtraOptionsPanel.Disable();
            PlayerExtraOptionsPanel.OptionsChanged += PlayerExtraOptionsOptionsChanged;
            BtnPlayerExtraOptionsOpen.LeftClick += BtnPlayerExtraOptionsLeftClick;
        }

        CheckDisallowedSides();
    }

    protected bool IsFavoriteMapsSelected() => DdGameModeMapFilter.SelectedItem?.Text == favoriteMapsLabel;

    /// <summary>
    /// Override this in a derived class to kick players.
    /// </summary>
    /// <param name="playerIndex">The index of the player that should be kicked.</param>
    protected virtual void KickPlayer(int playerIndex)
    {
        // Do nothing by default
    }

    protected void ListMaps()
    {
        LbGameModeMapList.SelectedIndexChanged -= LbGameModeMapList_SelectedIndexChanged;

        LbGameModeMapList.ClearItems();
        LbGameModeMapList.SetTopIndex(0);

        LbGameModeMapList.SelectedIndex = -1;

        int mapIndex = -1;
        int skippedMapsCount = 0;

        bool isFavoriteMapsSelected = IsFavoriteMapsSelected();
        List<GameModeMap> maps = GetSortedGameModeMaps();

        for (int i = 0; i < maps.Count; i++)
        {
            GameModeMap gameModeMap = maps[i];
            if (TbMapSearch.Text != TbMapSearch.Suggestion)
            {
                if (!gameModeMap.Map.Name.ToUpper().Contains(TbMapSearch.Text.ToUpper()))
                {
                    skippedMapsCount++;
                    continue;
                }
            }

            XNAListBoxItem rankItem = new()
            {
                Texture = gameModeMap.Map.IsCoop
                    ? StatisticsManager.Instance.HasBeatCoOpMap(gameModeMap.Map.Name, gameModeMap.GameMode.UIName)
                        ? RankTextures[Math.Abs(2 - gameModeMap.GameMode.CoopDifficultyLevel) + 1]
                        : RankTextures[0]
                    : RankTextures[GetDefaultMapRankIndex(gameModeMap) + 1]
            };

            XNAListBoxItem mapNameItem = new();
            string mapNameText = gameModeMap.Map.Name;
            if (isFavoriteMapsSelected)
                mapNameText += $" - {gameModeMap.GameMode.UIName}";

            mapNameItem.Text = Renderer.GetSafeString(mapNameText, LbGameModeMapList.FontIndex);

            if ((gameModeMap.Map.MultiplayerOnly || gameModeMap.GameMode.MultiplayerOnly) && !isMultiplayer)
                mapNameItem.TextColor = UISettings.ActiveSettings.DisabledItemColor;
            mapNameItem.Tag = gameModeMap;

            XNAListBoxItem[] mapInfoArray =
            {
                rankItem,
                mapNameItem,
            };

            LbGameModeMapList.AddItem(mapInfoArray);

            if (gameModeMap == GameModeMap)
                mapIndex = i - skippedMapsCount;
        }

        if (mapIndex > -1)
        {
            LbGameModeMapList.SelectedIndex = mapIndex;
            while (mapIndex > LbGameModeMapList.LastIndex)
                LbGameModeMapList.TopIndex++;
        }

        LbGameModeMapList.SelectedIndexChanged += LbGameModeMapList_SelectedIndexChanged;
    }

    protected void LoadDefaultGameModeMap()
    {
        if (DdGameModeMapFilter.Items.Count > 0)
        {
            DdGameModeMapFilter.SelectedIndex = GetDefaultGameModeMapFilterIndex();

            LbGameModeMapList.SelectedIndex = 0;
        }
    }

    protected virtual void OnGameOptionChanged()
    {
        CheckDisallowedSides();

        BtnLaunchGame.SetRank(GetRank());
    }

    protected virtual void PlayerExtraOptionsOptionsChanged(object sender, EventArgs e)
    {
        PlayerExtraOptions playerExtraOptions = GetPlayerExtraOptions();

        for (int i = 0; i < DdPlayerSides.Length; i++)
            EnablePlayerOptionDropDown(DdPlayerSides[i], i, !playerExtraOptions.IsForceRandomSides);

        for (int i = 0; i < DdPlayerTeams.Length; i++)
            EnablePlayerOptionDropDown(DdPlayerTeams[i], i, !playerExtraOptions.IsForceRandomTeams);

        for (int i = 0; i < DdPlayerColors.Length; i++)
            EnablePlayerOptionDropDown(DdPlayerColors[i], i, !playerExtraOptions.IsForceRandomColors);

        for (int i = 0; i < DdPlayerStarts.Length; i++)
            EnablePlayerOptionDropDown(DdPlayerStarts[i], i, !playerExtraOptions.IsForceRandomStarts);

        UpdateMapPreviewBoxEnabledStatus();
        RefreshBtnPlayerExtraOptionsOpenTexture();
    }

    /// <summary>
    /// Randomizes options of both human and AI players and returns the options as an array of PlayerHouseInfos.
    /// </summary>
    /// <param name="teamStartMappings">teamStartMappings.</param>
    /// <returns>An array of PlayerHouseInfos.</returns>
    protected virtual PlayerHouseInfo[] Randomize(List<TeamStartMapping> teamStartMappings)
    {
        int totalPlayerCount = Players.Count + AIPlayers.Count;
        PlayerHouseInfo[] houseInfos = new PlayerHouseInfo[totalPlayerCount];

        for (int i = 0; i < totalPlayerCount; i++)
            houseInfos[i] = new PlayerHouseInfo();

        // Gather list of spectators
        for (int i = 0; i < Players.Count; i++)
            houseInfos[i].IsSpectator = Players[i].SideId == GetSpectatorSideIndex();

        // Gather list of available colors
        List<int> freeColors = new();

        for (int cId = 0; cId < MPColors.Count; cId++)
            freeColors.Add(cId);

        if (Map.CoopInfo != null)
        {
            foreach (int colorIndex in Map.CoopInfo.DisallowedPlayerColors)
                _ = freeColors.Remove(colorIndex);
        }

        foreach (PlayerInfo player in Players)
            _ = freeColors.Remove(player.ColorId - 1); // The first color is Random

        foreach (PlayerInfo aiPlayer in AIPlayers)
            _ = freeColors.Remove(aiPlayer.ColorId - 1);

        // Gather list of available starting locations
        List<int> freeStartingLocations = new();
        List<int> takenStartingLocations = new();

        for (int i = 0; i < Map.MaxPlayers; i++)
            freeStartingLocations.Add(i);

        for (int i = 0; i < Players.Count; i++)
        {
            if (!houseInfos[i].IsSpectator)
            {
                _ = freeStartingLocations.Remove(Players[i].StartingLocation - 1);

                //takenStartingLocations.Add(Players[i].StartingLocation - 1);
                // ^ Gives everyone with a selected location a completely random
                // location in-game, because PlayerHouseInfo.RandomizeStart already
                // fills the list itself
            }
        }

        for (int i = 0; i < AIPlayers.Count; i++)
            _ = freeStartingLocations.Remove(AIPlayers[i].StartingLocation - 1);

        foreach (TeamStartMapping teamStartMapping in teamStartMappings.Where(mapping => mapping.IsBlock))
            _ = freeStartingLocations.Remove(teamStartMapping.StartingWaypoint);

        // Randomize options
        Random random = new(RandomSeed);

        for (int i = 0; i < totalPlayerCount; i++)
        {
            PlayerInfo pInfo;
            PlayerHouseInfo pHouseInfo = houseInfos[i];

            pInfo = i < Players.Count ? Players[i] : AIPlayers[i - Players.Count];

            pHouseInfo.RandomizeSide(pInfo, SideCount, random, GetDisallowedSides(), RandomSelectors, RandomSelectorCount);

            pHouseInfo.RandomizeColor(pInfo, freeColors, MPColors, random);
            pHouseInfo.RandomizeStart(pInfo, random, freeStartingLocations, takenStartingLocations, teamStartMappings.Any());
        }

        return houseInfos;
    }

    protected void RefreshForFavoriteMapRemoved()
    {
        if (!GameModeMapFilter.GetGameModeMaps().Any())
        {
            LoadDefaultGameModeMap();
            return;
        }

        ListMaps();
        if (IsFavoriteMapsSelected())
            LbGameModeMapList.SelectedIndex = 0; // the map was removed while viewing favorites
    }

    /// <summary>
    /// Refreshes the map selection UI to match the currently selected map and game mode.
    /// </summary>
    protected void RefreshMapSelectionUI()
    {
        if (GameMode == null)
            return;

        int gameModeMapFilterIndex = DdGameModeMapFilter.Items.FindIndex(i => i.Text == GameMode.UIName);

        if (gameModeMapFilterIndex == -1)
            return;

        if (DdGameModeMapFilter.SelectedIndex == gameModeMapFilterIndex)
            DdGameModeMapFilterSelectedIndexChanged(this, EventArgs.Empty);

        DdGameModeMapFilter.SelectedIndex = gameModeMapFilterIndex;
    }

    /// <summary>
    /// Resets Discord Rich Presence to default state.
    /// </summary>
    protected void ResetDiscordPresence() => DiscordHandler?.UpdatePresence();

    protected void SetPlayerExtraOptions(PlayerExtraOptions playerExtraOptions) => PlayerExtraOptionsPanel?.SetPlayerExtraOptions(playerExtraOptions);

    /// <summary>
    /// Writes spawn.ini, writes the map file, initializes statistics and starts the game process.
    /// </summary>
    protected virtual void StartGame()
    {
        PlayerHouseInfo[] houseInfos = WriteSpawnIni();
        InitializeMatchStatistics(houseInfos);
        WriteMap(houseInfos);

        GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

        GameProcessLogic.StartGameProcess();
        UpdateDiscordPresence(true);
    }

    protected virtual void ToggleFavoriteMap()
    {
        GameModeMap.IsFavorite = UserINISettings.Instance.ToggleFavoriteMap(Map.Name, GameMode.Name, GameModeMap.IsFavorite);
        MapPreviewBox.RefreshFavoriteBtn();
    }

    /// <summary>
    /// Updates Discord Rich Presence with actual information.
    /// </summary>
    /// <param name="resetTimer">Whether to restart the "Elapsed" timer or not.</param>
    protected abstract void UpdateDiscordPresence(bool resetTimer = false);

    /// <summary>
    /// Updates the enabled status of starting location selectors in the map preview box.
    /// </summary>
    protected abstract void UpdateMapPreviewBoxEnabledStatus();

    /// <summary>
    /// Override this in a derived class to write game lobby specific code to spawn.ini. For
    /// example, CnCNet game lobbies should write tunnel info in this method.
    /// </summary>
    /// <param name="iniFile">The spawn INI file.</param>
    protected virtual void WriteSpawnIniAdditions(IniFile iniFile)
    {
        // Do nothing by default
    }

    private static bool CanRightClickMultiplayer(XNADropDownItem selectedPlayer)
    {
        return selectedPlayer != null &&
               selectedPlayer.Text != ProgramConstants.PLAYERNAME &&
               !ProgramConstants.AIPLAYERNAMES.Contains(selectedPlayer.Text);
    }

    private static XNADropDownItem CreateGameFilterItem(string text, GameModeMapFilter filter)
    {
        return new XNADropDownItem
        {
            Text = text,
            Tag = filter
        };
    }

    private static Texture2D LoadTextureOrNull(string name) =>
            AssetLoader.AssetExists(name) ? AssetLoader.LoadTexture(name) : null;

    private void AddPlayerExtraOptionForcedNotice(bool disabled, string type)
            => AddNotice(disabled ?
                string.Format("The game host has disabled {0}".L10N("UI:Main:HostDisableSection"), type) :
                string.Format("The game host has enabled {0}".L10N("UI:Main:HostEnableSection"), type));

    private void ApplyForcedCheckBoxOptions(
            List<GameLobbyCheckBox> optionList,
            List<KeyValuePair<string, bool>> forcedOptions)
    {
        foreach (KeyValuePair<string, bool> option in forcedOptions)
        {
            GameLobbyCheckBox checkBox = CheckBoxes.Find(chk => chk.Name == option.Key);
            if (checkBox != null)
            {
                checkBox.Checked = option.Value;
                checkBox.AllowChecking = false;
                _ = optionList.Remove(checkBox);
            }
        }
    }

    private void ApplyForcedDropDownOptions(
            List<GameLobbyDropDown> optionList,
            List<KeyValuePair<string, int>> forcedOptions)
    {
        foreach (KeyValuePair<string, int> option in forcedOptions)
        {
            GameLobbyDropDown dropDown = DropDowns.Find(dd => dd.Name == option.Key);
            if (dropDown != null)
            {
                dropDown.SelectedIndex = option.Value;
                dropDown.AllowDropDown = false;
                _ = optionList.Remove(dropDown);
            }
        }
    }

    private void BtnMapSortAlphabetically_LeftClick(object sender, EventArgs e)
    {
        UserINISettings.Instance.MapSortState.Value = (int)BtnMapSortAlphabetically.GetState();

        RefreshMapSortAlphabeticallyBtn();
        UserINISettings.Instance.SaveSettings();
        ListMaps();
    }

    private void BtnPickRandomMap_LeftClick(object sender, EventArgs e) => PickRandomMap();

    private bool CanDeleteMap()
    {
        return Map != null && !Map.Official && !isMultiplayer;
    }

    private void ChkBox_CheckedChanged(object sender, EventArgs e)
    {
        if (disableGameOptionUpdateBroadcast)
            return;

        GameLobbyCheckBox checkBox = (GameLobbyCheckBox)sender;
        checkBox.HostChecked = checkBox.Checked;
        OnGameOptionChanged();
    }

    private void DeleteMapConfirmation()
    {
        if (Map == null)
            return;

        XNAMessageBox messageBox = XNAMessageBox.ShowYesNoDialog(
            WindowManager,
            "Delete Confirmation".L10N("UI:Main:DeleteMapConfirmTitle"),
            string.Format(
                "Are you sure you wish to delete the custom map {0}?".L10N("UI:Main:DeleteMapConfirmText"),
                Map.Name));
        messageBox.YesClickedAction = DeleteSelectedMap;
    }

    private void DeleteSelectedMap(XNAMessageBox messageBox)
    {
        try
        {
            MapLoader.DeleteCustomMap(GameModeMap);

            TbMapSearch.Text = string.Empty;
            if (GameMode.Maps.Count == 0)
            {
                // this will trigger another GameMode to be selected
                GameModeMap = GameModeMaps.Find(gm => gm.GameMode.Maps.Count > 0);
            }
            else
            {
                // this will trigger another Map to be selected
                LbGameModeMapList.SelectedIndex = LbGameModeMapList.SelectedIndex == 0 ? 1 : LbGameModeMapList.SelectedIndex - 1;
            }

            ListMaps();
            ChangeMap(GameModeMap);
        }
        catch (IOException ex)
        {
            Logger.Log($"Deleting map {Map.BaseFilePath} failed! Message: {ex.Message}");
            XNAMessageBox.Show(
                WindowManager,
                "Deleting Map Failed".L10N("UI:Main:DeleteMapFailedTitle"),
                "Deleting map failed! Reason:".L10N("UI:Main:DeleteMapFailedText") + " " + ex.Message);
        }
    }

    private void Dropdown_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (disableGameOptionUpdateBroadcast)
            return;

        GameLobbyDropDown dd = (GameLobbyDropDown)sender;
        dd.HostSelectedIndex = dd.SelectedIndex;
        OnGameOptionChanged();
    }

    private void EnablePlayerOptionDropDown(XNAClientDropDown clientDropDown, int playerIndex, bool enable)
    {
        PlayerInfo pInfo = GetPlayerInfoForIndex(playerIndex);
        bool allowOtherPlayerOptionsChange = AllowPlayerOptionsChange() && pInfo != null;
        clientDropDown.AllowDropDown = enable && (allowOtherPlayerOptionsChange || pInfo?.Name == ProgramConstants.PLAYERNAME);
        if (!clientDropDown.AllowDropDown)
            clientDropDown.SelectedIndex = clientDropDown.SelectedIndex > 0 ? 0 : clientDropDown.SelectedIndex;
    }

    private void GameProcessExited_Callback() => AddCallback(new Action(GameProcessExited), null);

    private XNALabel GeneratePlayerOptionCaption(string name, string text, int x, int y)
    {
        XNALabel label = new(WindowManager)
        {
            Name = name,
            Text = text,
            FontIndex = 1,
            ClientRectangle = new Rectangle(x, y, 0, 0)
        };
        PlayerOptionsPanel.AddChild(label);

        return label;
    }

    private List<GameModeMap> GetFavoriteGameModeMaps() =>
            GameModeMaps.Where(gmm => gmm.IsFavorite).ToList();

    private Func<List<GameModeMap>> GetGameModeMaps(GameMode gm) => () =>
            GameModeMaps.Where(gmm => gmm.GameMode == gm).ToList();

    private List<Map> GetMapList(int playerCount)
    {
        List<Map> mapList = new(GameMode.Maps.Where(x => x.MaxPlayers == playerCount));
        if (mapList.Count < 1 && playerCount <= MAXPLAYERCOUNT)
            return GetMapList(playerCount + 1);
        else
            return mapList;
    }

    /// <summary>
    /// Returns the number of teams with human players in them. Does not count spectators and human
    /// players that don't have a team set.
    /// </summary>
    /// <returns>The number of human player teams in the game.</returns>
    private int GetPvPTeamCount()
    {
        int[] teamPlayerCounts = new int[4];
        int playerTeamCount = 0;

        foreach (PlayerInfo pInfo in Players)
        {
            if (pInfo.IsAI || IsPlayerSpectator(pInfo))
                continue;

            if (pInfo.TeamId > 0)
            {
                teamPlayerCounts[pInfo.TeamId - 1]++;
                if (teamPlayerCounts[pInfo.TeamId - 1] == 2)
                    playerTeamCount++;
            }
        }

        return playerTeamCount;
    }

    /// <summary>
    /// Loads random side selectors from GameOptions.ini.
    /// </summary>
    /// <param name="selectorNames">TODO comment.</param>
    /// <param name="selectorSides">selectorSides.</param>
    private void GetRandomSelectors(List<string> selectorNames, List<int[]> selectorSides)
    {
        List<string> keys = GameOptionsIni.GetSectionKeys("RandomSelectors");

        if (keys == null)
            return;

        foreach (string randomSelector in keys)
        {
            List<int> randomSides = new();
            try
            {
                string[] tmp = GameOptionsIni.GetStringValue("RandomSelectors", randomSelector, string.Empty).Split(',');
                randomSides = Array.ConvertAll(tmp, int.Parse).Distinct().ToList();
                _ = randomSides.RemoveAll(x => x >= SideCount || x < 0);
            }
            catch (FormatException)
            {
            }

            if (randomSides.Count > 1)
            {
                selectorNames.Add(randomSelector);
                selectorSides.Add(randomSides.ToArray());
            }
        }
    }

    private List<GameModeMap> GetSortedGameModeMaps()
    {
        List<GameModeMap> gameModeMaps = GameModeMapFilter.GetGameModeMaps();

        // Only apply sort if the map list sort button is available.
        if (BtnMapSortAlphabetically.Enabled && BtnMapSortAlphabetically.Visible)
        {
            switch ((SortDirection)UserINISettings.Instance.MapSortState.Value)
            {
                case SortDirection.Asc:
                    gameModeMaps = gameModeMaps.OrderBy(gmm => gmm.Map.Name).ToList();
                    break;

                case SortDirection.Desc:
                    gameModeMaps = gameModeMaps.OrderByDescending(gmm => gmm.Map.Name).ToList();
                    break;
            }
        }

        return gameModeMaps;
    }

    private int GetSpectatorSideIndex() => SideCount + RandomSelectorCount;

    /// <summary>
    /// Until the GUICreator can handle typed classes, this must remain manually done.
    /// </summary>
    private void InitBtnMapSort()
    {
        BtnMapSortAlphabetically = new XNAClientStateButton<SortDirection>(WindowManager, new Dictionary<SortDirection, Texture2D>()
        {
            { SortDirection.None, AssetLoader.LoadTexture("sortAlphaNone.png") },
            { SortDirection.Asc, AssetLoader.LoadTexture("sortAlphaAsc.png") },
            { SortDirection.Desc, AssetLoader.LoadTexture("sortAlphaDesc.png") },
        });
        BtnMapSortAlphabetically.Name = nameof(BtnMapSortAlphabetically);
        BtnMapSortAlphabetically.ClientRectangle = new Rectangle(
            DdGameModeMapFilter.X + -DdGameModeMapFilter.Height - 4,
            DdGameModeMapFilter.Y,
            DdGameModeMapFilter.Height,
            DdGameModeMapFilter.Height);
        BtnMapSortAlphabetically.LeftClick += BtnMapSortAlphabetically_LeftClick;
        BtnMapSortAlphabetically.SetToolTipText("Sort Maps Alphabetically".L10N("UI:Main:MapSortAlphabeticallyToolTip"));
        RefreshMapSortAlphabeticallyBtn();
        AddChild(BtnMapSortAlphabetically);

        // Allow repositioning / disabling in INI.
        ReadINIForControl(BtnMapSortAlphabetically);
    }

    private void InitializeGameOptionPresetUI()
    {
        BtnSaveLoadGameOptions = FindChild<XNAClientButton>(nameof(BtnSaveLoadGameOptions), true);

        if (BtnSaveLoadGameOptions != null)
        {
            loadOrSaveGameOptionPresetWindow = new LoadOrSaveGameOptionPresetWindow(WindowManager);
            loadOrSaveGameOptionPresetWindow.Name = nameof(loadOrSaveGameOptionPresetWindow);
            loadOrSaveGameOptionPresetWindow.PresetLoaded += (sender, s) => HandleGameOptionPresetLoadCommand(s);
            loadOrSaveGameOptionPresetWindow.PresetSaved += (sender, s) => HandleGameOptionPresetSaveCommand(s);
            loadOrSaveGameOptionPresetWindow.Disable();
            XNAContextMenuItem loadConfigMenuItem = new()
            {
                Text = "Load".L10N("UI:Main:ButtonLoad"),
                SelectAction = () => loadOrSaveGameOptionPresetWindow.Show(true)
            };
            XNAContextMenuItem saveConfigMenuItem = new()
            {
                Text = "Save".L10N("UI:Main:ButtonSave"),
                SelectAction = () => loadOrSaveGameOptionPresetWindow.Show(false)
            };

            LoadSaveGameOptionsMenu = new XNAContextMenu(WindowManager);
            LoadSaveGameOptionsMenu.Name = nameof(LoadSaveGameOptionsMenu);
            LoadSaveGameOptionsMenu.ClientRectangle = new Rectangle(0, 0, 75, 0);
            LoadSaveGameOptionsMenu.Items.Add(loadConfigMenuItem);
            LoadSaveGameOptionsMenu.Items.Add(saveConfigMenuItem);

            BtnSaveLoadGameOptions.LeftClick += (sender, args) =>
                LoadSaveGameOptionsMenu.Open(GetCursorPoint());

            AddChild(LoadSaveGameOptionsMenu);
            AddChild(loadOrSaveGameOptionPresetWindow);
        }
    }

    private void InitializeMatchStatistics(PlayerHouseInfo[] houseInfos)
    {
        matchStatistics = new MatchStatistics(
            ProgramConstants.GameVersion,
            UniqueGameID,
            Map.Name,
            GameMode.UIName,
            Players.Count,
            Map.IsCoop);

        bool isValidForStar = true;
        foreach (GameLobbyCheckBox checkBox in CheckBoxes)
        {
            if ((checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && checkBox.Checked) ||
                (checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && !checkBox.Checked))
            {
                isValidForStar = false;
                break;
            }
        }

        matchStatistics.IsValidForStar = isValidForStar;

        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];
            matchStatistics.AddPlayer(
                pInfo.Name,
                pInfo.Name == ProgramConstants.PLAYERNAME,
                false,
                pInfo.SideId == SideCount + RandomSelectorCount,
                houseInfos[pId].SideIndex + 1,
                pInfo.TeamId,
                MPColors.FindIndex(c => c.GameColorIndex == houseInfos[pId].ColorIndex),
                10);
        }

        for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
        {
            PlayerHouseInfo pHouseInfo = houseInfos[Players.Count + aiId];
            PlayerInfo aiInfo = AIPlayers[aiId];
            matchStatistics.AddPlayer(
                "Computer",
                false,
                true,
                false,
                pHouseInfo.SideIndex + 1,
                aiInfo.TeamId,
                MPColors.FindIndex(c => c.GameColorIndex == pHouseInfo.ColorIndex),
                aiInfo.ReversedAILevel);
        }
    }

    /// <summary>
    /// Checks whether the specified player has selected Spectator as their side.
    /// </summary>
    /// <param name="pInfo">The player.</param>
    /// <returns>True if the player is a spectator, otherwise false.</returns>
    private bool IsPlayerSpectator(PlayerInfo pInfo)
    {
        if (pInfo.SideId == GetSpectatorSideIndex())
            return true;

        return false;
    }

    private void LbGameModeMapList_RightClick(object sender, EventArgs e)
    {
        if (LbGameModeMapList.HoveredIndex < 0 || LbGameModeMapList.HoveredIndex >= LbGameModeMapList.ItemCount)
            return;

        LbGameModeMapList.SelectedIndex = LbGameModeMapList.HoveredIndex;

        if (!MapContextMenu.Items.Any(i => i.VisibilityChecker == null || i.VisibilityChecker()))
            return;

        toggleFavoriteItem.Text = GameModeMap.IsFavorite ? "Remove Favorite".L10N("UI:Main:RemoveFavorite") : "Add Favorite".L10N("UI:Main:AddFavorite");

        MapContextMenu.Open(GetCursorPoint());
    }

    private void LbGameModeMapList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (LbGameModeMapList.SelectedIndex < 0 || LbGameModeMapList.SelectedIndex >= LbGameModeMapList.ItemCount)
        {
            ChangeMap(null);
            return;
        }

        XNAListBoxItem item = LbGameModeMapList.GetItem(1, LbGameModeMapList.SelectedIndex);

        GameModeMap = (GameModeMap)item.Tag;

        ChangeMap(GameModeMap);
    }

    private void ManipulateStartingLocations(IniFile mapIni, PlayerHouseInfo[] houseInfos)
    {
        if (RemoveStartingLocations)
        {
            if (Map.EnforceMaxPlayers)
                return;

            // All random starting locations given by the game
            IniSection waypointSection = mapIni.GetSection("Waypoints");
            if (waypointSection == null)
                return;

            // TODO implement IniSection.RemoveKey in Rampastring.Tools, then remove implementation
            // that depends on internal implementation of IniSection
            for (int i = 0; i <= 7; i++)
            {
                int index = waypointSection.Keys.FindIndex(k => !string.IsNullOrEmpty(k.Key) && k.Key == i.ToString());
                if (index > -1)
                    waypointSection.Keys.RemoveAt(index);
            }
        }

        // Multiple players cannot properly share the same starting location without breaking the
        // SpawnX house logic that pre-placed objects depend on

        // To work around this, we add new starting locations that just point to the same cell
        // coordinates as existing stacked starting locations and make additional players in the
        // same start loc start from the new starting locations instead.

        // As an additional restriction, players can only start from waypoints 0 to 7. That means
        // that if the map already has too many starting waypoints, we need to move existing (but
        // un-occupied) starting waypoints to point to the stacked locations so we can spawn the
        // players there.

        // Check for stacked starting locations (locations with more than 1 player on it)
        bool[] startingLocationUsed = new bool[MAXPLAYERCOUNT];
        bool stackedStartingLocations = false;
        foreach (PlayerHouseInfo houseInfo in houseInfos)
        {
            if (houseInfo.RealStartingWaypoint > -1)
            {
                startingLocationUsed[houseInfo.RealStartingWaypoint] = true;

                // If assigned starting waypoint is unknown while the real starting location is
                // known, it means that the location is shared with another player
                if (houseInfo.StartingWaypoint == -1)
                {
                    stackedStartingLocations = true;
                }
            }
        }

        // If any starting location is stacked, re-arrange all starting locations so that unused
        // starting locations are removed and made to point at used starting locations
        if (!stackedStartingLocations)
            return;

        // We also need to modify spawn.ini because WriteSpawnIni doesn't handle stacked positions.
        // We could move this code there, but then we'd have to process the stacked locations in two
        // places (here and in WriteSpawnIni) because we'd need to modify the map anyway. Not sure
        // whether having it like this or in WriteSpawnIni is better, but this implementation is
        // quicker to write for now.
        IniFile spawnIni = new(ProgramConstants.GamePath + ProgramConstants.SPAWNERSETTINGS);

        // For each player, check if they're sharing the starting location with someone else If they
        // are, find an unused waypoint and assign their starting location to match that
        for (int pId = 0; pId < houseInfos.Length; pId++)
        {
            PlayerHouseInfo houseInfo = houseInfos[pId];

            if (houseInfo.RealStartingWaypoint > -1 &&
                houseInfo.StartingWaypoint == -1)
            {
                // Find first unused starting location index
                int unusedLocation = -1;
                for (int i = 0; i < startingLocationUsed.Length; i++)
                {
                    if (!startingLocationUsed[i])
                    {
                        unusedLocation = i;
                        startingLocationUsed[i] = true;
                        break;
                    }
                }

                houseInfo.StartingWaypoint = unusedLocation;
                mapIni.SetIntValue(
                    "Waypoints",
                    unusedLocation.ToString(),
                    mapIni.GetIntValue("Waypoints", houseInfo.RealStartingWaypoint.ToString(), 0));
                spawnIni.SetIntValue("SpawnLocations", $"Multi{pId + 1}", unusedLocation);
            }
        }

        spawnIni.WriteIniFile();
    }

    private void MapPreviewBox_ToggleFavorite(object sender, EventArgs e) =>
        ToggleFavoriteMap();

    private void MultiplayerName_RightClick(object sender, EventArgs e)
    {
        XNADropDownItem selectedPlayer = ((XNADropDown)sender).SelectedItem;
        if (!GameLobbyBase.CanRightClickMultiplayer(selectedPlayer))
            return;

        if (selectedPlayer == null ||
            selectedPlayer.Text == ProgramConstants.PLAYERNAME)
        {
            return;
        }

        MultiplayerNameRightClicked?.Invoke(this, new MultiplayerNameRightClickedEventArgs(selectedPlayer.Text));
    }

    private void PickRandomMap()
    {
        int totalPlayerCount = Players.Count(p => p.SideId < DdPlayerSides[0].Items.Count - 1)
               + AIPlayers.Count;
        List<Map> maps = GetMapList(totalPlayerCount);
        if (maps.Count < 1)
            return;

        int random = new Random().Next(0, maps.Count);
        GameModeMap = GameModeMaps.Find(gmm => gmm.GameMode == GameMode && gmm.Map == maps[random]);

        Logger.Log("PickRandomMap: Rolled " + random + " out of " + maps.Count + ". Picked map: " + Map.Name);

        ChangeMap(GameModeMap);
        TbMapSearch.Text = string.Empty;
        TbMapSearch.OnSelectedChanged();
        ListMaps();
    }

    private void RefreshBtnPlayerExtraOptionsOpenTexture()
    {
        if (BtnPlayerExtraOptionsOpen != null)
        {
            string textureName = GetPlayerExtraOptions().IsDefault() ? "optionsButton.png" : "optionsButtonActive.png";
            string hoverTextureName = GetPlayerExtraOptions().IsDefault() ? "optionsButton_c.png" : "optionsButtonActive_c.png";
            Texture2D hoverTexture = AssetLoader.AssetExists(hoverTextureName) ? AssetLoader.LoadTexture(hoverTextureName) : null;
            BtnPlayerExtraOptionsOpen.IdleTexture = AssetLoader.LoadTexture(textureName);
            BtnPlayerExtraOptionsOpen.HoverTexture = hoverTexture;
        }
    }

    private void RefreshMapSortAlphabeticallyBtn()
    {
        if (Enum.IsDefined(typeof(SortDirection), UserINISettings.Instance.MapSortState.Value))
            BtnMapSortAlphabetically.SetState((SortDirection)UserINISettings.Instance.MapSortState.Value);
    }

    private void TbMapSearch_InputReceived(object sender, EventArgs e) => ListMaps();

    /// <summary>
    /// Writes spawnmap.ini.
    /// </summary>
    private void WriteMap(PlayerHouseInfo[] houseInfos)
    {
        File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNMAPINI);

        Logger.Log("Writing map.");

        Logger.Log("Loading map INI from " + Map.CompleteFilePath);

        IniFile mapIni = Map.GetMapIni();

        IniFile globalCodeIni = new(ProgramConstants.GamePath + "INI/Map Code/GlobalCode.ini");

        MapCodeHelper.ApplyMapCode(mapIni, GameMode.GetMapRulesIniFile());
        MapCodeHelper.ApplyMapCode(mapIni, globalCodeIni);

        if (isMultiplayer)
        {
            IniFile mpGlobalCodeIni = new(ProgramConstants.GamePath + "INI/Map Code/MultiplayerGlobalCode.ini");
            MapCodeHelper.ApplyMapCode(mapIni, mpGlobalCodeIni);
        }

        foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            checkBox.ApplyMapCode(mapIni, GameMode);

        foreach (GameLobbyDropDown dropDown in DropDowns)
            dropDown.ApplyMapCode(mapIni, GameMode);

        mapIni.MoveSectionToFirst("MultiplayerDialogSettings"); // Required by YR

        ManipulateStartingLocations(mapIni, houseInfos);

        mapIni.WriteIniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNMAPINI);
    }

    /// <summary>
    /// Writes spawn.ini. Returns the player house info returned from the randomizer.
    /// </summary>
    private PlayerHouseInfo[] WriteSpawnIni()
    {
        Logger.Log("Writing spawn.ini");

        File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNERSETTINGS);

        if (Map.IsCoop)
        {
            foreach (PlayerInfo pInfo in Players)
                pInfo.TeamId = 1;

            foreach (PlayerInfo pInfo in AIPlayers)
                pInfo.TeamId = 1;
        }

        List<TeamStartMapping> teamStartMappings = new(0);
        if (PlayerExtraOptionsPanel != null)
        {
            teamStartMappings = PlayerExtraOptionsPanel.GetTeamStartMappings();
        }

        PlayerHouseInfo[] houseInfos = Randomize(teamStartMappings);

        IniFile spawnIni = new(ProgramConstants.GamePath + ProgramConstants.SPAWNERSETTINGS);

        IniSection settings = new("Settings");

        settings.SetStringValue("Name", ProgramConstants.PLAYERNAME);
        settings.SetStringValue("Scenario", ProgramConstants.SPAWNMAPINI);
        settings.SetStringValue("UIGameMode", GameMode.UIName);
        settings.SetStringValue("UIMapName", Map.Name);
        settings.SetIntValue("PlayerCount", Players.Count);
        int myIndex = Players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME);
        settings.SetIntValue("Side", houseInfos[myIndex].InternalSideIndex);
        settings.SetBooleanValue("IsSpectator", houseInfos[myIndex].IsSpectator);
        settings.SetIntValue("Color", houseInfos[myIndex].ColorIndex);
        settings.SetStringValue("CustomLoadScreen", LoadingScreenController.GetLoadScreenName(houseInfos[myIndex].InternalSideIndex.ToString()));
        settings.SetIntValue("AIPlayers", AIPlayers.Count);
        settings.SetIntValue("Seed", RandomSeed);
        if (GetPvPTeamCount() > 1)
            settings.SetBooleanValue("CoachMode", true);
        if (GetGameType() == GameType.Coop)
            settings.SetBooleanValue("AutoSurrender", false);
        spawnIni.AddSection(settings);
        WriteSpawnIniAdditions(spawnIni);

        foreach (GameLobbyCheckBox chkBox in CheckBoxes)
            chkBox.ApplySpawnINICode(spawnIni);

        foreach (GameLobbyDropDown dd in DropDowns)
            dd.ApplySpawnIniCode(spawnIni);

        // Apply forced options from GameOptions.ini
        List<string> forcedKeys = GameOptionsIni.GetSectionKeys("ForcedSpawnIniOptions");

        if (forcedKeys != null)
        {
            foreach (string key in forcedKeys)
            {
                spawnIni.SetStringValue(
                    "Settings",
                    key,
                    GameOptionsIni.GetStringValue("ForcedSpawnIniOptions", key, string.Empty));
            }
        }

        GameMode.ApplySpawnIniCode(spawnIni); // Forced options from the game mode
        Map.ApplySpawnIniCode(
            spawnIni,
            Players.Count + AIPlayers.Count,
            AIPlayers.Count,
            GameMode.CoopDifficultyLevel); // Forced options from the map

        // Player options
        int otherId = 1;

        for (int pId = 0; pId < Players.Count; pId++)
        {
            PlayerInfo pInfo = Players[pId];
            PlayerHouseInfo pHouseInfo = houseInfos[pId];

            if (pInfo.Name == ProgramConstants.PLAYERNAME)
                continue;

            string sectionName = "Other" + otherId;

            spawnIni.SetStringValue(sectionName, "Name", pInfo.Name);
            spawnIni.SetIntValue(sectionName, "Side", pHouseInfo.InternalSideIndex);
            spawnIni.SetBooleanValue(sectionName, "IsSpectator", pHouseInfo.IsSpectator);
            spawnIni.SetIntValue(sectionName, "Color", pHouseInfo.ColorIndex);
            spawnIni.SetStringValue(sectionName, "Ip", GetIPAddressForPlayer(pInfo));
            spawnIni.SetIntValue(sectionName, "Port", pInfo.Port);

            otherId++;
        }

        // The spawner assigns players to SpawnX houses based on their in-game color index
        List<int> multiCmbIndexes = new();
        List<MultiplayerColor> sortedColorList = MPColors.OrderBy(mpc => mpc.GameColorIndex).ToList();

        for (int cId = 0; cId < sortedColorList.Count; cId++)
        {
            for (int pId = 0; pId < Players.Count; pId++)
            {
                if (houseInfos[pId].ColorIndex == sortedColorList[cId].GameColorIndex)
                    multiCmbIndexes.Add(pId);
            }
        }

        if (AIPlayers.Count > 0)
        {
            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                int multiId = multiCmbIndexes.Count + aiId + 1;

                string keyName = "Multi" + multiId;

                spawnIni.SetIntValue("HouseHandicaps", keyName, AIPlayers[aiId].AILevel);
                spawnIni.SetIntValue("HouseCountries", keyName, houseInfos[Players.Count + aiId].InternalSideIndex);
                spawnIni.SetIntValue("HouseColors", keyName, houseInfos[Players.Count + aiId].ColorIndex);
            }
        }

        for (int multiId = 0; multiId < multiCmbIndexes.Count; multiId++)
        {
            int pIndex = multiCmbIndexes[multiId];
            if (houseInfos[pIndex].IsSpectator)
                spawnIni.SetBooleanValue("IsSpectator", "Multi" + (multiId + 1), true);
        }

        // Write alliances, the code is pretty big so let's take it to another class
        AllianceHolder.WriteInfoToSpawnIni(Players, AIPlayers, multiCmbIndexes, houseInfos.ToList(), teamStartMappings, spawnIni);

        for (int pId = 0; pId < Players.Count; pId++)
        {
            int startingWaypoint = houseInfos[multiCmbIndexes[pId]].StartingWaypoint;

            // -1 means no starting location at all - let the game itself pick the starting location
            // using its own logic
            if (startingWaypoint > -1)
            {
                int multiIndex = pId + 1;
                spawnIni.SetIntValue(
                    "SpawnLocations",
                    "Multi" + multiIndex,
                    startingWaypoint);
            }
        }

        for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
        {
            int startingWaypoint = houseInfos[Players.Count + aiId].StartingWaypoint;

            if (startingWaypoint > -1)
            {
                int multiIndex = Players.Count + aiId + 1;
                spawnIni.SetIntValue(
                    "SpawnLocations",
                    "Multi" + multiIndex,
                    startingWaypoint);
            }
        }

        spawnIni.WriteIniFile();

        return houseInfos;
    }
}