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
/// A generic base for all game lobbies (Skirmish, LAN and CnCNet).
/// Contains the common logic for parsing game options and handling player info.
/// </summary>
public abstract class GameLobbyBase : INItializableWindow
{
    public List<GameLobbyCheckBox> CheckBoxes = new();

    protected const int MAXPLAYERCOUNT = 8;
    protected const int PLAYEROPTIONVERTICALMARGIN = 12;
    protected const int PLAYEROPTIONHORIZONTALMARGIN = 3;
    protected const int PLAYEROPTIONCAPTIONY = 6;
    protected readonly string BTN_LAUNCH_GAME = "Launch Game".L10N("UI:Main:ButtonLaunchGame");
    private const int DROP_DOWN_HEIGHT = 21;
    protected readonly string BTN_LAUNCH_READY = "I'm Ready".L10N("UI:Main:ButtonIAmReady");
    protected readonly string BTN_LAUNCH_NOT_READY = "Not Ready".L10N("UI:Main:ButtonNotReady");

    private const int RANK_NONE = 0;

    private readonly string favoriteMapsLabel = "Favorite Maps".L10N("UI:Main:FavoriteMaps");
    private const int RANK_HARD = 3;

    private readonly string _iniSectionName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameLobbyBase"/> class.
    /// Creates a new instance of the game lobby base.
    /// </summary>
    /// <param name="windowManager"></param>
    /// <param name="iniName">The name of the lobby in GameOptions.ini.</param>
    /// <param name="mapLoader"></param>
    /// <param name="isMultiplayer"></param>
    /// <param name="discordHandler"></param>
    public GameLobbyBase(
        WindowManager windowManager,
        string iniName,
        MapLoader mapLoader,
        bool isMultiplayer,
        DiscordHandler discordHandler)
        : base(windowManager)
    {
        _iniSectionName = iniName;
        this.mapLoader = mapLoader;
        this.isMultiplayer = isMultiplayer;
        this.discordHandler = discordHandler;
    }

    protected XNAPanel playerOptionsPanel;

    protected List<MultiplayerColor> mPColors;
    public List<GameLobbyDropDown> DropDowns = new();

    protected DiscordHandler discordHandler;

    protected MapLoader mapLoader;
    protected GameModeMapFilter gameModeMapFilter;

    protected XNAClientDropDown[] ddPlayerNames;

    /// <summary>
    /// Gets the list of multiplayer game mode maps.
    /// Each is an instance of a map for a specific game mode.
    /// </summary>
    protected GameModeMapCollection GameModeMaps => mapLoader.GameModeMaps;

    private GameModeMap _gameModeMap;

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

    protected Map Map => GameModeMap?.Map;

    protected GameMode GameMode => GameModeMap?.GameMode;
    protected XNAClientDropDown[] ddPlayerSides;
    protected XNAClientDropDown[] ddPlayerColors;
    protected XNAClientDropDown[] ddPlayerStarts;
    protected XNAClientDropDown[] ddPlayerTeams;

    protected XNAClientButton btnPlayerExtraOptionsOpen;
    protected PlayerExtraOptionsPanel playerExtraOptionsPanel;

    protected XNAClientButton btnLeaveGame;
    protected GameLaunchButton btnLaunchGame;
    protected XNAClientButton btnPickRandomMap;
    protected XNALabel lblMapName;
    protected XNALabel lblMapAuthor;
    protected XNALabel lblGameMode;
    protected XNALabel lblMapSize;

    protected MapPreviewBox mapPreviewBox;

    protected XNAMultiColumnListBox lbGameModeMapList;
    protected XNAClientDropDown ddGameModeMapFilter;
    protected XNALabel lblGameModeSelect;
    protected XNAContextMenu mapContextMenu;
    protected XNAClientStateButton<SortDirection> btnMapSortAlphabetically;

    private XNAContextMenuItem toggleFavoriteItem;

    protected XNASuggestionTextBox tbMapSearch;

    protected List<PlayerInfo> players = new();
    protected List<PlayerInfo> aIPlayers = new();

    protected Texture2D[] rankTextures;

    protected List<int[]> randomSelectors = new();

    protected EventHandler<MultiplayerNameRightClickedEventArgs> multiplayerNameRightClicked;

    protected bool PlayerUpdatingInProgress { get; set; }

    protected virtual PlayerInfo FindLocalPlayer() => players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

    /// <summary>
    /// Gets or sets the seed used for randomizing player options.
    /// </summary>
    protected int RandomSeed { get; set; }

    /// <summary>
    /// Gets or sets an unique identifier for this game.
    /// </summary>
    protected int UniqueGameID { get; set; }

    protected int SideCount { get; private set; }

    protected int RandomSelectorCount { get; private set; } = 1;

    private readonly bool isMultiplayer = false;

    private MatchStatistics matchStatistics;

    private bool disableGameOptionUpdateBroadcast = false;

    private LoadOrSaveGameOptionPresetWindow loadOrSaveGameOptionPresetWindow;

    /// <summary>
    /// Gets or sets a value indicating whether if set, the client will remove all starting waypoints from the map
    /// before launching it.
    /// </summary>
    protected bool RemoveStartingLocations { get; set; } = false;

    protected IniFile GameOptionsIni { get; private set; }

    protected XNAClientButton BtnSaveLoadGameOptions { get; set; }

    private XNAContextMenu loadSaveGameOptionsMenu { get; set; }

    public override void Initialize()
    {
        Name = _iniSectionName;

        //if (WindowManager.RenderResolutionY < 800)
        //    ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);
        //else
        ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX - 60, WindowManager.RenderResolutionY - 32);
        WindowManager.CenterControlOnScreen(this);
        BackgroundTexture = AssetLoader.LoadTexture("gamelobbybg.png");

        rankTextures = new Texture2D[4]
        {
            AssetLoader.LoadTexture("rankNone.png"),
            AssetLoader.LoadTexture("rankEasy.png"),
            AssetLoader.LoadTexture("rankNormal.png"),
            AssetLoader.LoadTexture("rankHard.png")
        };

        mPColors = MultiplayerColor.LoadColors();

        GameOptionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + "GameOptions.ini");

        base.Initialize();

        playerOptionsPanel = FindChild<XNAPanel>(nameof(playerOptionsPanel));

        btnLeaveGame = FindChild<XNAClientButton>(nameof(btnLeaveGame));
        btnLeaveGame.LeftClick += BtnLeaveGameLeftClick;

        btnLaunchGame = FindChild<GameLaunchButton>(nameof(btnLaunchGame));
        btnLaunchGame.LeftClick += BtnLaunchGameLeftClick;
        btnLaunchGame.InitStarDisplay(rankTextures);

        mapPreviewBox = FindChild<MapPreviewBox>("MapPreviewBox");
        mapPreviewBox.SetFields(players, aIPlayers, mPColors, GameOptionsIni.GetStringValue("General", "Sides", string.Empty).Split(','), GameOptionsIni);
        mapPreviewBox.ToggleFavorite += MapPreviewBox_ToggleFavorite;

        lblMapName = FindChild<XNALabel>(nameof(lblMapName));
        lblMapAuthor = FindChild<XNALabel>(nameof(lblMapAuthor));
        lblGameMode = FindChild<XNALabel>(nameof(lblGameMode));
        lblMapSize = FindChild<XNALabel>(nameof(lblMapSize));

        lbGameModeMapList = FindChild<XNAMultiColumnListBox>("lbMapList"); // lbMapList for backwards compatibility
        lbGameModeMapList.SelectedIndexChanged += LbGameModeMapList_SelectedIndexChanged;
        lbGameModeMapList.RightClick += LbGameModeMapList_RightClick;
        lbGameModeMapList.AllowKeyboardInput = true; //!isMultiplayer

        mapContextMenu = new XNAContextMenu(WindowManager);
        mapContextMenu.Name = nameof(mapContextMenu);
        mapContextMenu.Width = 100;
        mapContextMenu.AddItem("Delete Map".L10N("UI:Main:DeleteMap"), DeleteMapConfirmation, null, CanDeleteMap);
        toggleFavoriteItem = new XNAContextMenuItem
        {
            Text = "Favorite".L10N("UI:Main:Favorite"),
            SelectAction = ToggleFavoriteMap
        };
        mapContextMenu.AddItem(toggleFavoriteItem);
        AddChild(mapContextMenu);

        XNAPanel rankHeader = new(WindowManager)
        {
            BackgroundTexture = AssetLoader.LoadTexture("rank.png")
        };
        rankHeader.ClientRectangle = new Rectangle(0, 0, rankHeader.BackgroundTexture.Width,
            19);

        XNAListBox rankListBox = new(WindowManager)
        {
            TextBorderDistance = 2
        };

        lbGameModeMapList.AddColumn(rankHeader, rankListBox);
        lbGameModeMapList.AddColumn("MAP NAME".L10N("UI:Main:MapNameHeader"), lbGameModeMapList.Width - rankTextures[1].Width - 3);

        ddGameModeMapFilter = FindChild<XNAClientDropDown>("ddGameMode"); // ddGameMode for backwards compatibility
        ddGameModeMapFilter.SelectedIndexChanged += DdGameModeMapFilterSelectedIndexChanged;

        ddGameModeMapFilter.AddItem(CreateGameFilterItem(favoriteMapsLabel, new GameModeMapFilter(GetFavoriteGameModeMaps)));
        foreach (GameMode gm in GameModeMaps.GameModes)
            ddGameModeMapFilter.AddItem(CreateGameFilterItem(gm.UIName, new GameModeMapFilter(GetGameModeMaps(gm))));

        lblGameModeSelect = FindChild<XNALabel>(nameof(lblGameModeSelect));

        InitBtnMapSort();

        tbMapSearch = FindChild<XNASuggestionTextBox>(nameof(tbMapSearch));
        tbMapSearch.InputReceived += TbMapSearch_InputReceived;

        btnPickRandomMap = FindChild<XNAClientButton>(nameof(btnPickRandomMap));
        btnPickRandomMap.LeftClick += BtnPickRandomMap_LeftClick;

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

    protected bool IsFavoriteMapsSelected() => ddGameModeMapFilter.SelectedItem?.Text == favoriteMapsLabel;

    protected void HandleGameOptionPresetSaveCommand(GameOptionPresetEventArgs e) => HandleGameOptionPresetSaveCommand(e.PresetName);

    private static XNADropDownItem CreateGameFilterItem(string text, GameModeMapFilter filter)
    {
        return new XNADropDownItem
        {
            Text = text,
            Tag = filter
        };
    }

    /// <summary>
    /// Until the GUICreator can handle typed classes, this must remain manually done.
    /// </summary>
    private void InitBtnMapSort()
    {
        btnMapSortAlphabetically = new XNAClientStateButton<SortDirection>(WindowManager, new Dictionary<SortDirection, Texture2D>()
        {
            { SortDirection.None, AssetLoader.LoadTexture("sortAlphaNone.png") },
            { SortDirection.Asc, AssetLoader.LoadTexture("sortAlphaAsc.png") },
            { SortDirection.Desc, AssetLoader.LoadTexture("sortAlphaDesc.png") },
        });
        btnMapSortAlphabetically.Name = nameof(btnMapSortAlphabetically);
        btnMapSortAlphabetically.ClientRectangle = new Rectangle(
            ddGameModeMapFilter.X + -ddGameModeMapFilter.Height - 4, ddGameModeMapFilter.Y,
            ddGameModeMapFilter.Height, ddGameModeMapFilter.Height);
        btnMapSortAlphabetically.LeftClick += BtnMapSortAlphabetically_LeftClick;
        btnMapSortAlphabetically.SetToolTipText("Sort Maps Alphabetically".L10N("UI:Main:MapSortAlphabeticallyToolTip"));
        RefreshMapSortAlphabeticallyBtn();
        AddChild(btnMapSortAlphabetically);

        // Allow repositioning / disabling in INI.
        ReadINIForControl(btnMapSortAlphabetically);
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

            loadSaveGameOptionsMenu = new XNAContextMenu(WindowManager);
            loadSaveGameOptionsMenu.Name = nameof(loadSaveGameOptionsMenu);
            loadSaveGameOptionsMenu.ClientRectangle = new Rectangle(0, 0, 75, 0);
            loadSaveGameOptionsMenu.Items.Add(loadConfigMenuItem);
            loadSaveGameOptionsMenu.Items.Add(saveConfigMenuItem);

            BtnSaveLoadGameOptions.LeftClick += (sender, args) =>
                loadSaveGameOptionsMenu.Open(GetCursorPoint());

            AddChild(loadSaveGameOptionsMenu);
            AddChild(loadOrSaveGameOptionPresetWindow);
        }
    }

    private void BtnMapSortAlphabetically_LeftClick(object sender, EventArgs e)
    {
        UserINISettings.Instance.MapSortState.Value = (int)btnMapSortAlphabetically.GetState();

        RefreshMapSortAlphabeticallyBtn();
        UserINISettings.Instance.SaveSettings();
        ListMaps();
    }

    private void RefreshMapSortAlphabeticallyBtn()
    {
        if (Enum.IsDefined(typeof(SortDirection), UserINISettings.Instance.MapSortState.Value))
            btnMapSortAlphabetically.SetState((SortDirection)UserINISettings.Instance.MapSortState.Value);
    }

    private List<GameModeMap> GetFavoriteGameModeMaps() =>
        GameModeMaps.Where(gmm => gmm.IsFavorite).ToList();

    private Func<List<GameModeMap>> GetGameModeMaps(GameMode gm) => () =>
        GameModeMaps.Where(gmm => gmm.GameMode == gm).ToList();

    private void RefreshBtnPlayerExtraOptionsOpenTexture()
    {
        if (btnPlayerExtraOptionsOpen != null)
        {
            string textureName = GetPlayerExtraOptions().IsDefault() ? "optionsButton.png" : "optionsButtonActive.png";
            string hoverTextureName = GetPlayerExtraOptions().IsDefault() ? "optionsButton_c.png" : "optionsButtonActive_c.png";
            Texture2D hoverTexture = AssetLoader.AssetExists(hoverTextureName) ? AssetLoader.LoadTexture(hoverTextureName) : null;
            btnPlayerExtraOptionsOpen.IdleTexture = AssetLoader.LoadTexture(textureName);
            btnPlayerExtraOptionsOpen.HoverTexture = hoverTexture;
        }
    }

    protected void HandleGameOptionPresetSaveCommand(string presetName)
    {
        string error = AddGameOptionPreset(presetName);
        if (!string.IsNullOrEmpty(error))
            AddNotice(error);
    }

    protected void HandleGameOptionPresetLoadCommand(GameOptionPresetEventArgs e) => HandleGameOptionPresetLoadCommand(e.PresetName);

    protected void HandleGameOptionPresetLoadCommand(string presetName)
    {
        if (LoadGameOptionPreset(presetName))
            AddNotice("Game option preset loaded succesfully.".L10N("UI:Main:PresetLoaded"));
        else
            AddNotice(string.Format("Preset {0} not found!".L10N("UI:Main:PresetNotFound"), presetName));
    }

    protected void AddNotice(string message) => AddNotice(message, Color.White);

    protected abstract void AddNotice(string message, Color color);

    protected virtual void OnGameOptionChanged()
    {
        CheckDisallowedSides();

        btnLaunchGame.SetRank(GetRank());
    }

    private void BtnPickRandomMap_LeftClick(object sender, EventArgs e) => PickRandomMap();

    private void TbMapSearch_InputReceived(object sender, EventArgs e) => ListMaps();

    private void Dropdown_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (disableGameOptionUpdateBroadcast)
            return;

        GameLobbyDropDown dd = (GameLobbyDropDown)sender;
        dd.HostSelectedIndex = dd.SelectedIndex;
        OnGameOptionChanged();
    }

    private void ChkBox_CheckedChanged(object sender, EventArgs e)
    {
        if (disableGameOptionUpdateBroadcast)
            return;

        GameLobbyCheckBox checkBox = (GameLobbyCheckBox)sender;
        checkBox.HostChecked = checkBox.Checked;
        OnGameOptionChanged();
    }

    protected void DdGameModeMapFilterSelectedIndexChanged(object sender, EventArgs e)
    {
        gameModeMapFilter = ddGameModeMapFilter.SelectedItem.Tag as GameModeMapFilter;

        tbMapSearch.Text = string.Empty;
        tbMapSearch.OnSelectedChanged();

        ListMaps();

        if (lbGameModeMapList.SelectedIndex == -1)
            lbGameModeMapList.SelectedIndex = 0; // Select default GameModeMap
        else
            ChangeMap(GameModeMap);
    }

    protected void BtnPlayerExtraOptionsLeftClick(object sender, EventArgs e)
    {
        if (playerExtraOptionsPanel.Enabled)
            playerExtraOptionsPanel.Disable();
        else
            playerExtraOptionsPanel.Enable();
    }

    protected void ApplyPlayerExtraOptions(string sender, string message)
    {
        PlayerExtraOptions playerExtraOptions = PlayerExtraOptions.FromMessage(message);

        if (playerExtraOptions.IsForceRandomSides != playerExtraOptionsPanel.IsForcedRandomSides())
            AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomSides, "side selection".L10N("UI:Main:SideAsANoun"));

        if (playerExtraOptions.IsForceRandomColors != playerExtraOptionsPanel.IsForcedRandomColors())
            AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomColors, "color selection".L10N("UI:Main:ColorAsANoun"));

        if (playerExtraOptions.IsForceRandomStarts != playerExtraOptionsPanel.IsForcedRandomStarts())
            AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomStarts, "start selection".L10N("UI:Main:StartPositionAsANoun"));

        if (playerExtraOptions.IsForceRandomTeams != playerExtraOptionsPanel.IsForcedRandomTeams())
            AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomTeams, "team selection".L10N("UI:Main:TeamAsANoun"));

        if (playerExtraOptions.IsUseTeamStartMappings != playerExtraOptionsPanel.IsUseTeamStartMappings())
            AddPlayerExtraOptionForcedNotice(!playerExtraOptions.IsUseTeamStartMappings, "auto ally".L10N("UI:Main:AutoAllyAsANoun"));

        SetPlayerExtraOptions(playerExtraOptions);
        UpdateMapPreviewBoxEnabledStatus();
    }

    protected void ListMaps()
    {
        lbGameModeMapList.SelectedIndexChanged -= LbGameModeMapList_SelectedIndexChanged;

        lbGameModeMapList.ClearItems();
        lbGameModeMapList.SetTopIndex(0);

        lbGameModeMapList.SelectedIndex = -1;

        int mapIndex = -1;
        int skippedMapsCount = 0;

        bool isFavoriteMapsSelected = IsFavoriteMapsSelected();
        List<GameModeMap> maps = GetSortedGameModeMaps();

        for (int i = 0; i < maps.Count; i++)
        {
            GameModeMap gameModeMap = maps[i];
            if (tbMapSearch.Text != tbMapSearch.Suggestion)
            {
                if (!gameModeMap.Map.Name.ToUpper().Contains(tbMapSearch.Text.ToUpper()))
                {
                    skippedMapsCount++;
                    continue;
                }
            }

            XNAListBoxItem rankItem = new();
            rankItem.Texture = gameModeMap.Map.IsCoop
                ? StatisticsManager.Instance.HasBeatCoOpMap(gameModeMap.Map.Name, gameModeMap.GameMode.UIName)
                    ? rankTextures[Math.Abs(2 - gameModeMap.GameMode.CoopDifficultyLevel) + 1]
                    : rankTextures[0]
                : rankTextures[GetDefaultMapRankIndex(gameModeMap) + 1];

            XNAListBoxItem mapNameItem = new();
            string mapNameText = gameModeMap.Map.Name;
            if (isFavoriteMapsSelected)
                mapNameText += $" - {gameModeMap.GameMode.UIName}";

            mapNameItem.Text = Renderer.GetSafeString(mapNameText, lbGameModeMapList.FontIndex);

            if ((gameModeMap.Map.MultiplayerOnly || gameModeMap.GameMode.MultiplayerOnly) && !isMultiplayer)
                mapNameItem.TextColor = UISettings.ActiveSettings.DisabledItemColor;
            mapNameItem.Tag = gameModeMap;

            XNAListBoxItem[] mapInfoArray =
            {
                rankItem,
                mapNameItem,
            };

            lbGameModeMapList.AddItem(mapInfoArray);

            if (gameModeMap == GameModeMap)
                mapIndex = i - skippedMapsCount;
        }

        if (mapIndex > -1)
        {
            lbGameModeMapList.SelectedIndex = mapIndex;
            while (mapIndex > lbGameModeMapList.LastIndex)
                lbGameModeMapList.TopIndex++;
        }

        lbGameModeMapList.SelectedIndexChanged += LbGameModeMapList_SelectedIndexChanged;
    }

    private void AddPlayerExtraOptionForcedNotice(bool disabled, string type)
        => AddNotice(disabled ?
            string.Format("The game host has disabled {0}".L10N("UI:Main:HostDisableSection"), type) :
            string.Format("The game host has enabled {0}".L10N("UI:Main:HostEnableSection"), type));

    private List<GameModeMap> GetSortedGameModeMaps()
    {
        List<GameModeMap> gameModeMaps = gameModeMapFilter.GetGameModeMaps();

        // Only apply sort if the map list sort button is available.
        if (btnMapSortAlphabetically.Enabled && btnMapSortAlphabetically.Visible)
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

    protected abstract int GetDefaultMapRankIndex(GameModeMap gameModeMap);

    protected virtual void ToggleFavoriteMap()
    {
        GameModeMap.IsFavorite = UserINISettings.Instance.ToggleFavoriteMap(Map.Name, GameMode.Name, GameModeMap.IsFavorite);
        mapPreviewBox.RefreshFavoriteBtn();
    }

    private void LbGameModeMapList_RightClick(object sender, EventArgs e)
    {
        if (lbGameModeMapList.HoveredIndex < 0 || lbGameModeMapList.HoveredIndex >= lbGameModeMapList.ItemCount)
            return;

        lbGameModeMapList.SelectedIndex = lbGameModeMapList.HoveredIndex;

        if (!mapContextMenu.Items.Any(i => i.VisibilityChecker == null || i.VisibilityChecker()))
            return;

        toggleFavoriteItem.Text = GameModeMap.IsFavorite ? "Remove Favorite".L10N("UI:Main:RemoveFavorite") : "Add Favorite".L10N("UI:Main:AddFavorite");

        mapContextMenu.Open(GetCursorPoint());
    }

    private bool CanDeleteMap()
    {
        return Map != null && !Map.Official && !isMultiplayer;
    }

    private void DeleteMapConfirmation()
    {
        if (Map == null)
            return;

        XNAMessageBox messageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "Delete Confirmation".L10N("UI:Main:DeleteMapConfirmTitle"),
            string.Format("Are you sure you wish to delete the custom map {0}?".L10N("UI:Main:DeleteMapConfirmText"), Map.Name));
        messageBox.YesClickedAction = DeleteSelectedMap;
    }

    private void MapPreviewBox_ToggleFavorite(object sender, EventArgs e) =>
        ToggleFavoriteMap();

    protected void RefreshForFavoriteMapRemoved()
    {
        if (!gameModeMapFilter.GetGameModeMaps().Any())
        {
            LoadDefaultGameModeMap();
            return;
        }

        ListMaps();
        if (IsFavoriteMapsSelected())
            lbGameModeMapList.SelectedIndex = 0; // the map was removed while viewing favorites
    }

    /// <summary>
    /// Refreshes the map selection UI to match the currently selected map
    /// and game mode.
    /// </summary>
    protected void RefreshMapSelectionUI()
    {
        if (GameMode == null)
            return;

        int gameModeMapFilterIndex = ddGameModeMapFilter.Items.FindIndex(i => i.Text == GameMode.UIName);

        if (gameModeMapFilterIndex == -1)
            return;

        if (ddGameModeMapFilter.SelectedIndex == gameModeMapFilterIndex)
            DdGameModeMapFilterSelectedIndexChanged(this, EventArgs.Empty);

        ddGameModeMapFilter.SelectedIndex = gameModeMapFilterIndex;
    }

    private void DeleteSelectedMap(XNAMessageBox messageBox)
    {
        try
        {
            mapLoader.DeleteCustomMap(GameModeMap);

            tbMapSearch.Text = string.Empty;
            if (GameMode.Maps.Count == 0)
            {
                // this will trigger another GameMode to be selected
                GameModeMap = GameModeMaps.Find(gm => gm.GameMode.Maps.Count > 0);
            }
            else
            {
                // this will trigger another Map to be selected
                lbGameModeMapList.SelectedIndex = lbGameModeMapList.SelectedIndex == 0 ? 1 : lbGameModeMapList.SelectedIndex - 1;
            }

            ListMaps();
            ChangeMap(GameModeMap);
        }
        catch (IOException ex)
        {
            Logger.Log($"Deleting map {Map.BaseFilePath} failed! Message: {ex.Message}");
            XNAMessageBox.Show(WindowManager, "Deleting Map Failed".L10N("UI:Main:DeleteMapFailedTitle"),
                "Deleting map failed! Reason:".L10N("UI:Main:DeleteMapFailedText") + " " + ex.Message);
        }
    }

    private void LbGameModeMapList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbGameModeMapList.SelectedIndex < 0 || lbGameModeMapList.SelectedIndex >= lbGameModeMapList.ItemCount)
        {
            ChangeMap(null);
            return;
        }

        XNAListBoxItem item = lbGameModeMapList.GetItem(1, lbGameModeMapList.SelectedIndex);

        GameModeMap = (GameModeMap)item.Tag;

        ChangeMap(GameModeMap);
    }

    private void PickRandomMap()
    {
        int totalPlayerCount = players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
               + aIPlayers.Count;
        List<Map> maps = GetMapList(totalPlayerCount);
        if (maps.Count < 1)
            return;

        int random = new Random().Next(0, maps.Count);
        GameModeMap = GameModeMaps.Find(gmm => gmm.GameMode == GameMode && gmm.Map == maps[random]);

        Logger.Log("PickRandomMap: Rolled " + random + " out of " + maps.Count + ". Picked map: " + Map.Name);

        ChangeMap(GameModeMap);
        tbMapSearch.Text = string.Empty;
        tbMapSearch.OnSelectedChanged();
        ListMaps();
    }

    private List<Map> GetMapList(int playerCount)
    {
        List<Map> mapList = new(GameMode.Maps.Where(x => x.MaxPlayers == playerCount));
        if (mapList.Count < 1 && playerCount <= MAXPLAYERCOUNT)
            return GetMapList(playerCount + 1);
        else
            return mapList;
    }

    /// <summary>
    /// Initializes the player option drop-down controls.
    /// </summary>
    protected void InitPlayerOptionDropdowns()
    {
        ddPlayerNames = new XNAClientDropDown[MAXPLAYERCOUNT];
        ddPlayerSides = new XNAClientDropDown[MAXPLAYERCOUNT];
        ddPlayerColors = new XNAClientDropDown[MAXPLAYERCOUNT];
        ddPlayerStarts = new XNAClientDropDown[MAXPLAYERCOUNT];
        ddPlayerTeams = new XNAClientDropDown[MAXPLAYERCOUNT];

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
        GetRandomSelectors(selectorNames, randomSelectors);
        RandomSelectorCount = randomSelectors.Count + 1;
        mapPreviewBox.RandomSelectorCount = RandomSelectorCount;

        string randomColor = GameOptionsIni.GetStringValue("General", "RandomColor", "255,255,255");

        for (int i = MAXPLAYERCOUNT - 1; i > -1; i--)
        {
            XNAClientDropDown ddPlayerName = new(WindowManager)
            {
                Name = "ddPlayerName" + i,
                ClientRectangle = new Rectangle(
                    locationX,
                locationY + ((DROP_DOWN_HEIGHT + playerOptionVecticalMargin) * i),
                playerNameWidth, DROP_DOWN_HEIGHT)
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
                ddPlayerName.Y, sideWidth, DROP_DOWN_HEIGHT)
            };
            ddPlayerSide.AddItem("Random".L10N("UI:Main:RandomSide"), GameLobbyBase.LoadTextureOrNull("randomicon.png"));
            foreach (string randomSelector in selectorNames)
                ddPlayerSide.AddItem(randomSelector, GameLobbyBase.LoadTextureOrNull(randomSelector + "icon.png"));
            foreach (string sideName in sides)
                ddPlayerSide.AddItem(sideName, GameLobbyBase.LoadTextureOrNull(sideName + "icon.png"));
            ddPlayerSide.AllowDropDown = false;
            ddPlayerSide.SelectedIndexChanged += CopyPlayerDataFromUI;
            ddPlayerSide.Tag = true;

            XNAClientDropDown ddPlayerColor = new(WindowManager)
            {
                Name = "ddPlayerColor" + i,
                ClientRectangle = new Rectangle(
                ddPlayerSide.Right + playerOptionHorizontalMargin,
                ddPlayerName.Y, colorWidth, DROP_DOWN_HEIGHT)
            };
            ddPlayerColor.AddItem("Random".L10N("UI:Main:RandomColor"), AssetLoader.GetColorFromString(randomColor));
            foreach (MultiplayerColor mpColor in mPColors)
                ddPlayerColor.AddItem(mpColor.Name, mpColor.XnaColor);
            ddPlayerColor.AllowDropDown = false;
            ddPlayerColor.SelectedIndexChanged += CopyPlayerDataFromUI;
            ddPlayerColor.Tag = false;

            XNAClientDropDown ddPlayerTeam = new(WindowManager)
            {
                Name = "ddPlayerTeam" + i,
                ClientRectangle = new Rectangle(
                ddPlayerColor.Right + playerOptionHorizontalMargin,
                ddPlayerName.Y, teamWidth, DROP_DOWN_HEIGHT)
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
                ddPlayerName.Y, startWidth, DROP_DOWN_HEIGHT)
            };
            for (int j = 1; j < 9; j++)
                ddPlayerStart.AddItem(j.ToString());
            ddPlayerStart.AllowDropDown = false;
            ddPlayerStart.SelectedIndexChanged += CopyPlayerDataFromUI;
            ddPlayerStart.Visible = false;
            ddPlayerStart.Enabled = false;
            ddPlayerStart.Tag = true;

            ddPlayerNames[i] = ddPlayerName;
            ddPlayerSides[i] = ddPlayerSide;
            ddPlayerColors[i] = ddPlayerColor;
            ddPlayerStarts[i] = ddPlayerStart;
            ddPlayerTeams[i] = ddPlayerTeam;

            playerOptionsPanel.AddChild(ddPlayerName);
            playerOptionsPanel.AddChild(ddPlayerSide);
            playerOptionsPanel.AddChild(ddPlayerColor);
            playerOptionsPanel.AddChild(ddPlayerStart);
            playerOptionsPanel.AddChild(ddPlayerTeam);

            ReadINIForControl(ddPlayerName);
            ReadINIForControl(ddPlayerSide);
            ReadINIForControl(ddPlayerColor);
            ReadINIForControl(ddPlayerStart);
            ReadINIForControl(ddPlayerTeam);
        }

        XNALabel lblName = GeneratePlayerOptionCaption("lblName", "PLAYER".L10N("UI:Main:PlayerOptionPlayer"), ddPlayerNames[0].X, playerOptionCaptionLocationY);
        XNALabel lblSide = GeneratePlayerOptionCaption("lblSide", "SIDE".L10N("UI:Main:PlayerOptionSide"), ddPlayerSides[0].X, playerOptionCaptionLocationY);
        XNALabel lblColor = GeneratePlayerOptionCaption("lblColor", "COLOR".L10N("UI:Main:PlayerOptionColor"), ddPlayerColors[0].X, playerOptionCaptionLocationY);

        XNALabel lblStart = GeneratePlayerOptionCaption("lblStart", "START".L10N("UI:Main:PlayerOptionStart"), ddPlayerStarts[0].X, playerOptionCaptionLocationY);
        lblStart.Visible = false;

        XNALabel lblTeam = GeneratePlayerOptionCaption("lblTeam", "TEAM".L10N("UI:Main:PlayerOptionTeam"), ddPlayerTeams[0].X, playerOptionCaptionLocationY);

        ReadINIForControl(lblName);
        ReadINIForControl(lblSide);
        ReadINIForControl(lblColor);
        ReadINIForControl(lblStart);
        ReadINIForControl(lblTeam);

        btnPlayerExtraOptionsOpen = FindChild<XNAClientButton>(nameof(btnPlayerExtraOptionsOpen), true);
        if (btnPlayerExtraOptionsOpen != null)
        {
            playerExtraOptionsPanel = FindChild<PlayerExtraOptionsPanel>(nameof(playerExtraOptionsPanel));
            playerExtraOptionsPanel.Disable();
            playerExtraOptionsPanel.OptionsChanged += PlayerExtraOptionsOptionsChanged;
            btnPlayerExtraOptionsOpen.LeftClick += BtnPlayerExtraOptionsLeftClick;
        }

        CheckDisallowedSides();
    }

    protected virtual void PlayerExtraOptionsOptionsChanged(object sender, EventArgs e)
    {
        PlayerExtraOptions playerExtraOptions = GetPlayerExtraOptions();

        for (int i = 0; i < ddPlayerSides.Length; i++)
            EnablePlayerOptionDropDown(ddPlayerSides[i], i, !playerExtraOptions.IsForceRandomSides);

        for (int i = 0; i < ddPlayerTeams.Length; i++)
            EnablePlayerOptionDropDown(ddPlayerTeams[i], i, !playerExtraOptions.IsForceRandomTeams);

        for (int i = 0; i < ddPlayerColors.Length; i++)
            EnablePlayerOptionDropDown(ddPlayerColors[i], i, !playerExtraOptions.IsForceRandomColors);

        for (int i = 0; i < ddPlayerStarts.Length; i++)
            EnablePlayerOptionDropDown(ddPlayerStarts[i], i, !playerExtraOptions.IsForceRandomStarts);

        UpdateMapPreviewBoxEnabledStatus();
        RefreshBtnPlayerExtraOptionsOpenTexture();
    }

    protected PlayerInfo GetPlayerInfoForIndex(int playerIndex)
    {
        if (playerIndex < players.Count)
            return players[playerIndex];

        if (playerIndex < players.Count + aIPlayers.Count)
            return aIPlayers[playerIndex - players.Count];

        return null;
    }

    private XNALabel GeneratePlayerOptionCaption(string name, string text, int x, int y)
    {
        XNALabel label = new(WindowManager)
        {
            Name = name,
            Text = text,
            FontIndex = 1,
            ClientRectangle = new Rectangle(x, y, 0, 0)
        };
        playerOptionsPanel.AddChild(label);

        return label;
    }

    private void EnablePlayerOptionDropDown(XNAClientDropDown clientDropDown, int playerIndex, bool enable)
    {
        PlayerInfo pInfo = GetPlayerInfoForIndex(playerIndex);
        bool allowOtherPlayerOptionsChange = AllowPlayerOptionsChange() && pInfo != null;
        clientDropDown.AllowDropDown = enable && (allowOtherPlayerOptionsChange || pInfo?.Name == ProgramConstants.PLAYERNAME);
        if (!clientDropDown.AllowDropDown)
            clientDropDown.SelectedIndex = clientDropDown.SelectedIndex > 0 ? 0 : clientDropDown.SelectedIndex;
    }

    protected PlayerExtraOptions GetPlayerExtraOptions() =>
        playerExtraOptionsPanel == null ? new PlayerExtraOptions() : playerExtraOptionsPanel.GetPlayerExtraOptions();

    protected void SetPlayerExtraOptions(PlayerExtraOptions playerExtraOptions) => playerExtraOptionsPanel?.SetPlayerExtraOptions(playerExtraOptions);

    protected string GetTeamMappingsError() => GetPlayerExtraOptions()?.GetTeamMappingsError();

    protected abstract void BtnLaunchGameLeftClick(object sender, EventArgs e);

    private static Texture2D LoadTextureOrNull(string name) =>
        AssetLoader.AssetExists(name) ? AssetLoader.LoadTexture(name) : null;

    /// <summary>
    /// Loads random side selectors from GameOptions.ini.
    /// </summary>
    /// <param name="selectorNames">TODO comment.</param>
    /// <param name="selectorSides">TODO comment.</param>
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

    protected abstract void BtnLeaveGameLeftClick(object sender, EventArgs e);

    /// <summary>
    /// Updates Discord Rich Presence with actual information.
    /// </summary>
    /// <param name="resetTimer">Whether to restart the "Elapsed" timer or not.</param>
    protected abstract void UpdateDiscordPresence(bool resetTimer = false);

    /// <summary>
    /// Resets Discord Rich Presence to default state.
    /// </summary>
    protected void ResetDiscordPresence() => discordHandler?.UpdatePresence();

    protected void LoadDefaultGameModeMap()
    {
        if (ddGameModeMapFilter.Items.Count > 0)
        {
            ddGameModeMapFilter.SelectedIndex = GetDefaultGameModeMapFilterIndex();

            lbGameModeMapList.SelectedIndex = 0;
        }
    }

    protected int GetDefaultGameModeMapFilterIndex()
    {
        return ddGameModeMapFilter.Items.FindIndex(i => (i.Tag as GameModeMapFilter)?.Any() ?? false);
    }

    protected GameModeMapFilter GetDefaultGameModeMapFilter()
    {
        return ddGameModeMapFilter.Items[GetDefaultGameModeMapFilterIndex()].Tag as GameModeMapFilter;
    }

    /// <summary>
    /// Applies disallowed side indexes to the side option drop-downs
    /// and player options.
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

            foreach (XNADropDown dd in ddPlayerSides)
            {
                //dd.Items[0].Selectable = false;
                for (int i = 0; i < RandomSelectorCount; i++)
                    dd.Items[i].Selectable = false;
            }
        }
        else
        {
            foreach (XNADropDown dd in ddPlayerSides)
            {
                //dd.Items[0].Selectable = true;
                for (int i = 0; i < RandomSelectorCount; i++)
                    dd.Items[i].Selectable = true;
            }
        }

        IEnumerable<PlayerInfo> concatPlayerList = players.Concat(aIPlayers);

        // Disable custom random groups if all or all except one of included sides are unavailable.
        int c = 0;
        List<PlayerInfo> playerInfos = concatPlayerList.ToList();
        foreach (int[] randomSides in randomSelectors)
        {
            int disableCount = 0;

            foreach (int side in randomSides)
            {
                if (disallowedSideArray[side])
                    disableCount++;
            }

            bool disabled = disableCount >= randomSides.Length - 1;

            foreach (XNADropDown dd in ddPlayerSides)
                dd.Items[1 + c].Selectable = !disabled;

            foreach (PlayerInfo pInfo in playerInfos)
            {
                if (pInfo.SideId == 1 + c && disabled)
                    pInfo.SideId = defaultSide;
            }

            c++;
        }

        // Go over the side array and either disable or enable the side
        // dropdown options depending on whether the side is available
        for (int i = 0; i < disallowedSideArray.Length; i++)
        {
            bool disabled = disallowedSideArray[i];

            if (disabled)
            {
                foreach (XNADropDown dd in ddPlayerSides)
                    dd.Items[i + RandomSelectorCount].Selectable = false;

                // Change the sides of players that use the disabled
                // side to the default side
                foreach (PlayerInfo pInfo in playerInfos)
                {
                    if (pInfo.SideId == i + RandomSelectorCount)
                        pInfo.SideId = defaultSide;
                }
            }
            else
            {
                foreach (XNADropDown dd in ddPlayerSides)
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

            foreach (XNADropDown dd in ddPlayerSides)
            {
                if (dd.Items.Count > GetSpectatorSideIndex())
                    dd.Items[SideCount + RandomSelectorCount].Selectable = false;
            }
        }
        else
        {
            foreach (XNADropDown dd in ddPlayerSides)
            {
                if (dd.Items.Count > SideCount + RandomSelectorCount)
                    dd.Items[SideCount + RandomSelectorCount].Selectable = true;
            }
        }
    }

    private int GetSpectatorSideIndex() => SideCount + RandomSelectorCount;

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

    /// <summary>
    /// Randomizes options of both human and AI players
    /// and returns the options as an array of PlayerHouseInfos.
    /// </summary>
    /// <returns>An array of PlayerHouseInfos.</returns>
    protected virtual PlayerHouseInfo[] Randomize(List<TeamStartMapping> teamStartMappings)
    {
        int totalPlayerCount = players.Count + aIPlayers.Count;
        PlayerHouseInfo[] houseInfos = new PlayerHouseInfo[totalPlayerCount];

        for (int i = 0; i < totalPlayerCount; i++)
            houseInfos[i] = new PlayerHouseInfo();

        // Gather list of spectators
        for (int i = 0; i < players.Count; i++)
            houseInfos[i].IsSpectator = players[i].SideId == GetSpectatorSideIndex();

        // Gather list of available colors
        List<int> freeColors = new();

        for (int cId = 0; cId < mPColors.Count; cId++)
            freeColors.Add(cId);

        if (Map.CoopInfo != null)
        {
            foreach (int colorIndex in Map.CoopInfo.DisallowedPlayerColors)
                _ = freeColors.Remove(colorIndex);
        }

        foreach (PlayerInfo player in players)
            _ = freeColors.Remove(player.ColorId - 1); // The first color is Random

        foreach (PlayerInfo aiPlayer in aIPlayers)
            _ = freeColors.Remove(aiPlayer.ColorId - 1);

        // Gather list of available starting locations
        List<int> freeStartingLocations = new();
        List<int> takenStartingLocations = new();

        for (int i = 0; i < Map.MaxPlayers; i++)
            freeStartingLocations.Add(i);

        for (int i = 0; i < players.Count; i++)
        {
            if (!houseInfos[i].IsSpectator)
            {
                _ = freeStartingLocations.Remove(players[i].StartingLocation - 1);

                //takenStartingLocations.Add(Players[i].StartingLocation - 1);
                // ^ Gives everyone with a selected location a completely random
                // location in-game, because PlayerHouseInfo.RandomizeStart already
                // fills the list itself
            }
        }

        for (int i = 0; i < aIPlayers.Count; i++)
            _ = freeStartingLocations.Remove(aIPlayers[i].StartingLocation - 1);

        foreach (TeamStartMapping teamStartMapping in teamStartMappings.Where(mapping => mapping.IsBlock))
            _ = freeStartingLocations.Remove(teamStartMapping.StartingWaypoint);

        // Randomize options
        Random random = new(RandomSeed);

        for (int i = 0; i < totalPlayerCount; i++)
        {
            PlayerInfo pInfo;
            PlayerHouseInfo pHouseInfo = houseInfos[i];

            pInfo = i < players.Count ? players[i] : aIPlayers[i - players.Count];

            pHouseInfo.RandomizeSide(pInfo, SideCount, random, GetDisallowedSides(), randomSelectors, RandomSelectorCount);

            pHouseInfo.RandomizeColor(pInfo, freeColors, mPColors, random);
            pHouseInfo.RandomizeStart(pInfo, random, freeStartingLocations, takenStartingLocations, teamStartMappings.Any());
        }

        return houseInfos;
    }

    protected virtual string GetIPAddressForPlayer(PlayerInfo player) => "0.0.0.0";

    /// <summary>
    /// Writes spawn.ini. Returns the player house info returned from the randomizer.
    /// </summary>
    private PlayerHouseInfo[] WriteSpawnIni()
    {
        Logger.Log("Writing spawn.ini");

        File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNERSETTINGS);

        if (Map.IsCoop)
        {
            foreach (PlayerInfo pInfo in players)
                pInfo.TeamId = 1;

            foreach (PlayerInfo pInfo in aIPlayers)
                pInfo.TeamId = 1;
        }

        List<TeamStartMapping> teamStartMappings = new(0);
        if (playerExtraOptionsPanel != null)
        {
            teamStartMappings = playerExtraOptionsPanel.GetTeamStartMappings();
        }

        PlayerHouseInfo[] houseInfos = Randomize(teamStartMappings);

        IniFile spawnIni = new(ProgramConstants.GamePath + ProgramConstants.SPAWNERSETTINGS);

        IniSection settings = new("Settings");

        settings.SetStringValue("Name", ProgramConstants.PLAYERNAME);
        settings.SetStringValue("Scenario", ProgramConstants.SPAWNMAPINI);
        settings.SetStringValue("UIGameMode", GameMode.UIName);
        settings.SetStringValue("UIMapName", Map.Name);
        settings.SetIntValue("PlayerCount", players.Count);
        int myIndex = players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME);
        settings.SetIntValue("Side", houseInfos[myIndex].InternalSideIndex);
        settings.SetBooleanValue("IsSpectator", houseInfos[myIndex].IsSpectator);
        settings.SetIntValue("Color", houseInfos[myIndex].ColorIndex);
        settings.SetStringValue("CustomLoadScreen", LoadingScreenController.GetLoadScreenName(houseInfos[myIndex].InternalSideIndex.ToString()));
        settings.SetIntValue("AIPlayers", aIPlayers.Count);
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
                spawnIni.SetStringValue("Settings", key,
                    GameOptionsIni.GetStringValue("ForcedSpawnIniOptions", key, string.Empty));
            }
        }

        GameMode.ApplySpawnIniCode(spawnIni); // Forced options from the game mode
        Map.ApplySpawnIniCode(spawnIni, players.Count + aIPlayers.Count,
            aIPlayers.Count, GameMode.CoopDifficultyLevel); // Forced options from the map

        // Player options
        int otherId = 1;

        for (int pId = 0; pId < players.Count; pId++)
        {
            PlayerInfo pInfo = players[pId];
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
        List<MultiplayerColor> sortedColorList = mPColors.OrderBy(mpc => mpc.GameColorIndex).ToList();

        for (int cId = 0; cId < sortedColorList.Count; cId++)
        {
            for (int pId = 0; pId < players.Count; pId++)
            {
                if (houseInfos[pId].ColorIndex == sortedColorList[cId].GameColorIndex)
                    multiCmbIndexes.Add(pId);
            }
        }

        if (aIPlayers.Count > 0)
        {
            for (int aiId = 0; aiId < aIPlayers.Count; aiId++)
            {
                int multiId = multiCmbIndexes.Count + aiId + 1;

                string keyName = "Multi" + multiId;

                spawnIni.SetIntValue("HouseHandicaps", keyName, aIPlayers[aiId].AILevel);
                spawnIni.SetIntValue("HouseCountries", keyName, houseInfos[players.Count + aiId].InternalSideIndex);
                spawnIni.SetIntValue("HouseColors", keyName, houseInfos[players.Count + aiId].ColorIndex);
            }
        }

        for (int multiId = 0; multiId < multiCmbIndexes.Count; multiId++)
        {
            int pIndex = multiCmbIndexes[multiId];
            if (houseInfos[pIndex].IsSpectator)
                spawnIni.SetBooleanValue("IsSpectator", "Multi" + (multiId + 1), true);
        }

        // Write alliances, the code is pretty big so let's take it to another class
        AllianceHolder.WriteInfoToSpawnIni(players, aIPlayers, multiCmbIndexes, houseInfos.ToList(), teamStartMappings, spawnIni);

        for (int pId = 0; pId < players.Count; pId++)
        {
            int startingWaypoint = houseInfos[multiCmbIndexes[pId]].StartingWaypoint;

            // -1 means no starting location at all - let the game itself pick the starting location
            // using its own logic
            if (startingWaypoint > -1)
            {
                int multiIndex = pId + 1;
                spawnIni.SetIntValue("SpawnLocations", "Multi" + multiIndex,
                    startingWaypoint);
            }
        }

        for (int aiId = 0; aiId < aIPlayers.Count; aiId++)
        {
            int startingWaypoint = houseInfos[players.Count + aiId].StartingWaypoint;

            if (startingWaypoint > -1)
            {
                int multiIndex = players.Count + aiId + 1;
                spawnIni.SetIntValue("SpawnLocations", "Multi" + multiIndex,
                    startingWaypoint);
            }
        }

        spawnIni.WriteIniFile();

        return houseInfos;
    }

    /// <summary>
    /// Returns the number of teams with human players in them.
    /// Does not count spectators and human players that don't have a team set.
    /// </summary>
    /// <returns>The number of human player teams in the game.</returns>
    private int GetPvPTeamCount()
    {
        int[] teamPlayerCounts = new int[4];
        int playerTeamCount = 0;

        foreach (PlayerInfo pInfo in players)
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

    /// <summary>
    /// Override this in a derived class to write game lobby specific code to
    /// spawn.ini. For example, CnCNet game lobbies should write tunnel info
    /// in this method.
    /// </summary>
    /// <param name="iniFile">The spawn INI file.</param>
    protected virtual void WriteSpawnIniAdditions(IniFile iniFile)
    {
        // Do nothing by default
    }

    /// <summary>
    /// Writes spawn.ini, writes the map file, initializes statistics and
    /// starts the game process.
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

    protected virtual void GameProcessExited()
    {
        GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

        Logger.Log("GameProcessExited: Parsing statistics.");

        matchStatistics.ParseStatistics(ProgramConstants.GamePath, ClientConfiguration.Instance.LocalGame, false);

        Logger.Log("GameProcessExited: Adding match to statistics.");

        StatisticsManager.Instance.AddMatchAndSaveDatabase(true, matchStatistics);

        ClearReadyStatuses();

        CopyPlayerDataToUI();

        UpdateDiscordPresence(true);
    }

    private void InitializeMatchStatistics(PlayerHouseInfo[] houseInfos)
    {
        matchStatistics = new MatchStatistics(ProgramConstants.GAME_VERSION, UniqueGameID,
            Map.Name, GameMode.UIName, players.Count, Map.IsCoop);

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

        for (int pId = 0; pId < players.Count; pId++)
        {
            PlayerInfo pInfo = players[pId];
            matchStatistics.AddPlayer(pInfo.Name, pInfo.Name == ProgramConstants.PLAYERNAME,
                false, pInfo.SideId == SideCount + RandomSelectorCount, houseInfos[pId].SideIndex + 1, pInfo.TeamId,
                mPColors.FindIndex(c => c.GameColorIndex == houseInfos[pId].ColorIndex), 10);
        }

        for (int aiId = 0; aiId < aIPlayers.Count; aiId++)
        {
            PlayerHouseInfo pHouseInfo = houseInfos[players.Count + aiId];
            PlayerInfo aiInfo = aIPlayers[aiId];
            matchStatistics.AddPlayer("Computer", false, true, false,
                pHouseInfo.SideIndex + 1, aiInfo.TeamId,
                mPColors.FindIndex(c => c.GameColorIndex == pHouseInfo.ColorIndex),
                aiInfo.ReversedAILevel);
        }
    }

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

            // TODO implement IniSection.RemoveKey in Rampastring.Tools, then
            // remove implementation that depends on internal implementation
            // of IniSection
            for (int i = 0; i <= 7; i++)
            {
                int index = waypointSection.Keys.FindIndex(k => !string.IsNullOrEmpty(k.Key) && k.Key == i.ToString());
                if (index > -1)
                    waypointSection.Keys.RemoveAt(index);
            }
        }

        // Multiple players cannot properly share the same starting location
        // without breaking the SpawnX house logic that pre-placed objects depend on

        // To work around this, we add new starting locations that just point
        // to the same cell coordinates as existing stacked starting locations
        // and make additional players in the same start loc start from the new
        // starting locations instead.

        // As an additional restriction, players can only start from waypoints 0 to 7.
        // That means that if the map already has too many starting waypoints,
        // we need to move existing (but un-occupied) starting waypoints to point
        // to the stacked locations so we can spawn the players there.

        // Check for stacked starting locations (locations with more than 1 player on it)
        bool[] startingLocationUsed = new bool[MAXPLAYERCOUNT];
        bool stackedStartingLocations = false;
        foreach (PlayerHouseInfo houseInfo in houseInfos)
        {
            if (houseInfo.RealStartingWaypoint > -1)
            {
                startingLocationUsed[houseInfo.RealStartingWaypoint] = true;

                // If assigned starting waypoint is unknown while the real
                // starting location is known, it means that
                // the location is shared with another player
                if (houseInfo.StartingWaypoint == -1)
                {
                    stackedStartingLocations = true;
                }
            }
        }

        // If any starting location is stacked, re-arrange all starting locations
        // so that unused starting locations are removed and made to point at used
        // starting locations
        if (!stackedStartingLocations)
            return;

        // We also need to modify spawn.ini because WriteSpawnIni
        // doesn't handle stacked positions.
        // We could move this code there, but then we'd have to process
        // the stacked locations in two places (here and in WriteSpawnIni)
        // because we'd need to modify the map anyway.
        // Not sure whether having it like this or in WriteSpawnIni
        // is better, but this implementation is quicker to write for now.
        IniFile spawnIni = new(ProgramConstants.GamePath + ProgramConstants.SPAWNERSETTINGS);

        // For each player, check if they're sharing the starting location
        // with someone else
        // If they are, find an unused waypoint and assign their
        // starting location to match that
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
                mapIni.SetIntValue("Waypoints", unusedLocation.ToString(),
                    mapIni.GetIntValue("Waypoints", houseInfo.RealStartingWaypoint.ToString(), 0));
                spawnIni.SetIntValue("SpawnLocations", $"Multi{pId + 1}", unusedLocation);
            }
        }

        spawnIni.WriteIniFile();
    }

    private void GameProcessExited_Callback() => AddCallback(new Action(GameProcessExited), null);

    /// <summary>
    /// "Copies" player information from the UI to internal memory,
    /// applying users' player options changes.
    /// </summary>
    protected virtual void CopyPlayerDataFromUI(object sender, EventArgs e)
    {
        if (PlayerUpdatingInProgress)
            return;

        XNADropDown senderDropDown = (XNADropDown)sender;
        if ((bool)senderDropDown.Tag)
            ClearReadyStatuses();

        int? oldSideId = players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId;

        for (int pId = 0; pId < players.Count; pId++)
        {
            PlayerInfo pInfo = players[pId];

            pInfo.ColorId = ddPlayerColors[pId].SelectedIndex;
            pInfo.SideId = ddPlayerSides[pId].SelectedIndex;
            pInfo.StartingLocation = ddPlayerStarts[pId].SelectedIndex;
            pInfo.TeamId = ddPlayerTeams[pId].SelectedIndex;

            if (pInfo.SideId == SideCount + RandomSelectorCount)
                pInfo.StartingLocation = 0;

            XNADropDown ddName = ddPlayerNames[pId];

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

        aIPlayers.Clear();
        for (int cmbId = players.Count; cmbId < 8; cmbId++)
        {
            XNADropDown dd = ddPlayerNames[cmbId];
            dd.Items[0].Text = "-";

            if (dd.SelectedIndex < 1)
                continue;

            PlayerInfo aiPlayer = new()
            {
                Name = dd.Items[dd.SelectedIndex].Text,
                AILevel = 2 - (dd.SelectedIndex - 1),
                SideId = Math.Max(ddPlayerSides[cmbId].SelectedIndex, 0),
                ColorId = Math.Max(ddPlayerColors[cmbId].SelectedIndex, 0),
                StartingLocation = Math.Max(ddPlayerStarts[cmbId].SelectedIndex, 0),
                TeamId = Map != null && Map.IsCoop ? 1 : Math.Max(ddPlayerTeams[cmbId].SelectedIndex, 0),
                IsAI = true
            };

            aIPlayers.Add(aiPlayer);
        }

        CopyPlayerDataToUI();
        btnLaunchGame.SetRank(GetRank());

        if (oldSideId != players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId)
            UpdateDiscordPresence();
    }

    /// <summary>
    /// Sets the ready status of all non-host human players to false.
    /// </summary>
    /// <param name="resetAutoReady">If set, players with autoready enabled are reset as well.</param>
    protected void ClearReadyStatuses(bool resetAutoReady = false)
    {
        for (int i = 1; i < players.Count; i++)
        {
            if (resetAutoReady || !players[i].AutoReady || players[i].IsInGame)
                players[i].Ready = false;
        }
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
        for (int pId = 0; pId < players.Count; pId++)
        {
            PlayerInfo pInfo = players[pId];

            pInfo.Index = pId;

            XNADropDown ddPlayerName = ddPlayerNames[pId];
            ddPlayerName.Items[0].Text = pInfo.Name;
            ddPlayerName.Items[1].Text = string.Empty;
            ddPlayerName.Items[2].Text = "Kick".L10N("UI:Main:Kick");
            ddPlayerName.Items[3].Text = "Ban".L10N("UI:Main:Ban");
            ddPlayerName.SelectedIndex = 0;
            ddPlayerName.AllowDropDown = false;

            bool allowPlayerOptionsChange = allowOptionsChange || pInfo.Name == ProgramConstants.PLAYERNAME;

            ddPlayerSides[pId].SelectedIndex = pInfo.SideId;
            ddPlayerSides[pId].AllowDropDown = !playerExtraOptions.IsForceRandomSides && allowPlayerOptionsChange;

            ddPlayerColors[pId].SelectedIndex = pInfo.ColorId;
            ddPlayerColors[pId].AllowDropDown = !playerExtraOptions.IsForceRandomColors && allowPlayerOptionsChange;

            ddPlayerStarts[pId].SelectedIndex = pInfo.StartingLocation;

            ddPlayerTeams[pId].SelectedIndex = pInfo.TeamId;
            if (GameModeMap != null)
            {
                ddPlayerTeams[pId].AllowDropDown = !playerExtraOptions.IsForceRandomTeams && allowPlayerOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                ddPlayerStarts[pId].AllowDropDown = !playerExtraOptions.IsForceRandomStarts && allowPlayerOptionsChange && (Map.IsCoop || !Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations);
            }
        }

        // AI players
        for (int aiId = 0; aiId < aIPlayers.Count; aiId++)
        {
            PlayerInfo aiInfo = aIPlayers[aiId];

            int index = players.Count + aiId;

            aiInfo.Index = index;

            XNADropDown ddPlayerName = ddPlayerNames[index];
            ddPlayerName.Items[0].Text = "-";
            ddPlayerName.Items[1].Text = ProgramConstants.AIPLAYERNAMES[0];
            ddPlayerName.Items[2].Text = ProgramConstants.AIPLAYERNAMES[1];
            ddPlayerName.Items[3].Text = ProgramConstants.AIPLAYERNAMES[2];
            ddPlayerName.SelectedIndex = 3 - aiInfo.AILevel;
            ddPlayerName.AllowDropDown = allowOptionsChange;

            ddPlayerSides[index].SelectedIndex = aiInfo.SideId;
            ddPlayerSides[index].AllowDropDown = !playerExtraOptions.IsForceRandomSides && allowOptionsChange;

            ddPlayerColors[index].SelectedIndex = aiInfo.ColorId;
            ddPlayerColors[index].AllowDropDown = !playerExtraOptions.IsForceRandomColors && allowOptionsChange;

            ddPlayerStarts[index].SelectedIndex = aiInfo.StartingLocation;

            ddPlayerTeams[index].SelectedIndex = aiInfo.TeamId;

            if (GameModeMap != null)
            {
                ddPlayerTeams[index].AllowDropDown = !playerExtraOptions.IsForceRandomTeams && allowOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                ddPlayerStarts[index].AllowDropDown = !playerExtraOptions.IsForceRandomStarts && allowOptionsChange && (Map.IsCoop || !Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations);
            }
        }

        // Unused player slots
        for (int ddIndex = players.Count + aIPlayers.Count; ddIndex < MAXPLAYERCOUNT; ddIndex++)
        {
            XNADropDown ddPlayerName = ddPlayerNames[ddIndex];
            ddPlayerName.AllowDropDown = false;
            ddPlayerName.Items[0].Text = string.Empty;
            ddPlayerName.Items[1].Text = ProgramConstants.AIPLAYERNAMES[0];
            ddPlayerName.Items[2].Text = ProgramConstants.AIPLAYERNAMES[1];
            ddPlayerName.Items[3].Text = ProgramConstants.AIPLAYERNAMES[2];
            ddPlayerName.SelectedIndex = 0;

            ddPlayerSides[ddIndex].SelectedIndex = -1;
            ddPlayerSides[ddIndex].AllowDropDown = false;

            ddPlayerColors[ddIndex].SelectedIndex = -1;
            ddPlayerColors[ddIndex].AllowDropDown = false;

            ddPlayerStarts[ddIndex].SelectedIndex = -1;
            ddPlayerStarts[ddIndex].AllowDropDown = false;

            ddPlayerTeams[ddIndex].SelectedIndex = -1;
            ddPlayerTeams[ddIndex].AllowDropDown = false;
        }

        if (allowOptionsChange && players.Count + aIPlayers.Count < MAXPLAYERCOUNT)
            ddPlayerNames[players.Count + aIPlayers.Count].AllowDropDown = true;

        mapPreviewBox.UpdateStartingLocationTexts();
        UpdateMapPreviewBoxEnabledStatus();

        PlayerUpdatingInProgress = false;
    }

    private static bool CanRightClickMultiplayer(XNADropDownItem selectedPlayer)
    {
        return selectedPlayer != null &&
               selectedPlayer.Text != ProgramConstants.PLAYERNAME &&
               !ProgramConstants.AIPLAYERNAMES.Contains(selectedPlayer.Text);
    }

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

        multiplayerNameRightClicked?.Invoke(this, new MultiplayerNameRightClickedEventArgs(selectedPlayer.Text));
    }

    /// <summary>
    /// Updates the enabled status of starting location selectors
    /// in the map preview box.
    /// </summary>
    protected abstract void UpdateMapPreviewBoxEnabledStatus();

    /// <summary>
    /// Override this in a derived class to kick players.
    /// </summary>
    /// <param name="playerIndex">The index of the player that should be kicked.</param>
    protected virtual void KickPlayer(int playerIndex)
    {
        // Do nothing by default
    }

    /// <summary>
    /// Override this in a derived class to ban players.
    /// </summary>
    /// <param name="playerIndex">The index of the player that should be banned.</param>
    protected virtual void BanPlayer(int playerIndex)
    {
        // Do nothing by default
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
            lblMapName.Text = "Map: Unknown".L10N("UI:Main:MapUnknown");
            lblMapAuthor.Text = "By Unknown Author".L10N("UI:Main:AuthorByUnknown");
            lblGameMode.Text = "Game mode: Unknown".L10N("UI:Main:GameModeUnknown");
            lblMapSize.Text = "Size: Not available".L10N("UI:Main:MapSizeUnknown");

            mapPreviewBox.GameModeMap = null;

            return;
        }

        lblMapName.Text = "Map:".L10N("UI:Main:Map") + " " + Renderer.GetSafeString(Map.Name, lblMapName.FontIndex);
        lblMapAuthor.Text = "By".L10N("UI:Main:AuthorBy") + " " + Renderer.GetSafeString(Map.Author, lblMapAuthor.FontIndex);
        lblGameMode.Text = "Game mode:".L10N("UI:Main:GameModeLabel") + " " + GameMode.UIName;
        lblMapSize.Text = "Size:".L10N("UI:Main:MapSize") + " " + Map.GetSizeString();

        disableGameOptionUpdateBroadcast = true;

        // Clear forced options
        foreach (GameLobbyDropDown ddGameOption in DropDowns)
            ddGameOption.AllowDropDown = true;

        foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            checkBox.AllowChecking = true;

        // We could either pass the CheckBoxes and DropDowns of this class
        // to the Map and GameMode instances and let them apply their forced
        // options, or we could do it in this class with helper functions.
        // The second approach is probably clearer.

        // We use these temp lists to determine which options WERE NOT forced
        // by the map. We then return these to user-defined settings.
        // This prevents forced options from one map getting carried
        // to other maps.
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
        foreach (XNAClientDropDown ddSide in ddPlayerSides)
        {
            ddSide.Items.ForEach(item => item.Selectable = true);
        }

        // Enable all colors by default
        foreach (XNAClientDropDown ddColor in ddPlayerColors)
        {
            ddColor.Items.ForEach(item => item.Selectable = true);
        }

        // Apply starting locations
        foreach (XNAClientDropDown ddStart in ddPlayerStarts)
        {
            ddStart.Items.Clear();

            ddStart.AddItem("???");

            for (int i = 1; i <= Map.MaxPlayers; i++)
                ddStart.AddItem(i.ToString());
        }

        // Check if AI players allowed
        bool aIAllowed = !(Map.MultiplayerOnly || GameMode.MultiplayerOnly) ||
                         !(Map.HumanPlayersOnly || GameMode.HumanPlayersOnly);
        foreach (XNAClientDropDown ddName in ddPlayerNames)
        {
            if (ddName.Items.Count > 3)
            {
                ddName.Items[1].Selectable = aIAllowed;
                ddName.Items[2].Selectable = aIAllowed;
                ddName.Items[3].Selectable = aIAllowed;
            }
        }

        if (!aIAllowed)
            aIPlayers.Clear();
        IEnumerable<PlayerInfo> concatPlayerList = players.Concat(aIPlayers).ToList();

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
                if (disallowedColorIndex >= mPColors.Count)
                    continue;

                foreach (XNADropDown ddColor in ddPlayerColors)
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

        mapPreviewBox.GameModeMap = GameModeMap;
        CopyPlayerDataToUI();

        disableGameOptionUpdateBroadcast = false;

        playerExtraOptionsPanel?.UpdateForMap(Map);
    }

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

    protected GameType GetGameType()
    {
        int teamCount = GetPvPTeamCount();

        if (teamCount == 0)
            return GameType.FFA;

        if (teamCount == 1)
            return GameType.Coop;

        return GameType.TeamGame;
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

        PlayerInfo localPlayer = players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

        if (localPlayer == null)
            return RANK_NONE;

        if (IsPlayerSpectator(localPlayer))
            return RANK_NONE;

        // These variables are used by both the skirmish and multiplayer code paths
        int[] teamMemberCounts = new int[5];
        int lowestEnemyAILevel = 2;
        int highestAllyAILevel = 0;

        foreach (PlayerInfo aiPlayer in aIPlayers)
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
            if (players.Count == 1)
                return RANK_NONE;

            // PvP stars for 2-player and 3-player maps
            if (Map.MaxPlayers <= 3)
            {
                List<PlayerInfo> filteredPlayers = players.Where(p => !IsPlayerSpectator(p)).ToList();

                if (aIPlayers.Count > 0)
                    return RANK_NONE;

                if (filteredPlayers.Count != Map.MaxPlayers)
                    return RANK_NONE;

                int localTeamIndex = localPlayer.TeamId;
                if (localTeamIndex > 0 && filteredPlayers.Count(p => p.TeamId == localTeamIndex) > 1)
                    return RANK_NONE;

                return RANK_HARD;
            }

            // Coop stars for maps with 4 or more players
            // See the code in StatisticsManager.GetRankForCoopMatch for the conditions
            if (players.Find(IsPlayerSpectator) != null)
                return RANK_NONE;

            if (aIPlayers.Count == 0)
                return RANK_NONE;

            if (players.Find(p => p.TeamId != localPlayer.TeamId) != null)
                return RANK_NONE;

            if (players.Find(p => p.TeamId == 0) != null)
                return RANK_NONE;

            if (aIPlayers.Find(p => p.TeamId == 0) != null)
                return RANK_NONE;

            teamMemberCounts[localPlayer.TeamId] += players.Count;

            if (lowestEnemyAILevel < highestAllyAILevel)
            {
                // Check that the player's AI allies aren't stronger
                return RANK_NONE;
            }

            // Check that all teams have at least as many players
            // as the human players' team
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

        // *********
        // Skirmish!
        // *********
        if (aIPlayers.Count != Map.MaxPlayers - 1)
            return RANK_NONE;

        teamMemberCounts[localPlayer.TeamId]++;

        if (lowestEnemyAILevel < highestAllyAILevel)
        {
            // Check that the player's AI allies aren't stronger
            return RANK_NONE;
        }

        if (localPlayer.TeamId > 0)
        {
            // Check that all teams have at least as many players
            // as the local player's team
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

    protected abstract bool AllowPlayerOptionsChange();
}