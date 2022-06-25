using System;
using System.Collections.Generic;
using System.IO;
using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// An abstract base class for a multiplayer game loading lobby.
/// </summary>
public abstract class GameLoadingLobbyBase : XNAWindow, ISwitchable
{
    private FileSystemWatcher fsw;

    private DateTime gameLoadTime;

    private bool isSettingUp = false;

    private XNALabel lblGameMode;

    private XNALabel lblMapName;

    private XNALabel lblSavedGameTime;

    private List<MultiplayerColor> mPColors = new();

    private int uniqueGameId = 0;

    public GameLoadingLobbyBase(WindowManager windowManager, DiscordHandler discordHandler)
            : base(windowManager)
    {
        this.DiscordHandler = discordHandler;
    }

    public event EventHandler GameLeft;

    protected XNAClientButton BtnLeaveGame { get; set; }

    protected XNAClientButton BtnLoadGame { get; set; }

    protected XNAClientDropDown DdSavedGame { get; set; }

    protected DiscordHandler DiscordHandler { get; set; }

    protected bool IsHost { get; set; } = false;

    protected ChatListBox LbChatMessages { get; set; }

    protected XNALabel LblDescription { get; set; }

    protected XNALabel LblGameModeValue { get; set; }

    protected XNALabel LblMapNameValue { get; set; }

    protected XNALabel[] LblPlayerNames { get; set; }

    protected XNAPanel PanelPlayers { get; set; }

    /// <summary>
    /// Gets or sets the list of players in the game lobby.
    /// </summary>
    protected List<PlayerInfo> Players { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of players in the current saved game.
    /// </summary>
    protected List<SavedGamePlayer> SGPlayers { get; set; } = new();

    protected EnhancedSoundEffect SndGetReadySound { get; set; }

    protected EnhancedSoundEffect SndJoinSound { get; set; }

    protected EnhancedSoundEffect SndLeaveSound { get; set; }

    protected EnhancedSoundEffect SndMessageSound { get; set; }

    protected XNATextBox TbChatInput { get; set; }

    public override void Draw(GameTime gameTime)
    {
        Renderer.FillRectangle(
            new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY),
            new Color(0, 0, 0, 255));

        base.Draw(gameTime);
    }

    public abstract string GetSwitchName();

    public override void Initialize()
    {
        Name = "GameLoadingLobby";
        ClientRectangle = new Rectangle(0, 0, 590, 510);
        BackgroundTexture = AssetLoader.LoadTexture("loadmpsavebg.png");

        LblDescription = new XNALabel(WindowManager);
        LblDescription.Name = nameof(LblDescription);
        LblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
        LblDescription.Text = "Wait for all players to join and get ready, then click Load Game to load the saved multiplayer game.".L10N("UI:Main:LobbyInitialTip");

        PanelPlayers = new XNAPanel(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 32, 373, 125),
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1),
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED
        };

        AddChild(LblDescription);
        AddChild(PanelPlayers);

        LblPlayerNames = new XNALabel[8];
        for (int i = 0; i < 8; i++)
        {
            XNALabel lblPlayerName = new(WindowManager);
            lblPlayerName.Name = nameof(lblPlayerName) + i;

            lblPlayerName.ClientRectangle = i < 4 ? new Rectangle(9, 9 + (30 * i), 0, 0) : new Rectangle(190, 9 + (30 * (i - 4)), 0, 0);

            lblPlayerName.Text = string.Format("Player {0}".L10N("UI:Main:PlayerX"), i) + " ";
            PanelPlayers.AddChild(lblPlayerName);
            LblPlayerNames[i] = lblPlayerName;
        }

        lblMapName = new XNALabel(WindowManager);
        lblMapName.Name = nameof(lblMapName);
        lblMapName.FontIndex = 1;
        lblMapName.ClientRectangle = new Rectangle(
            PanelPlayers.Right + 12,
            PanelPlayers.Y,
            0,
            0);
        lblMapName.Text = "MAP:".L10N("UI:Main:MapLabel");

        LblMapNameValue = new XNALabel(WindowManager);
        LblMapNameValue.Name = nameof(LblMapNameValue);
        LblMapNameValue.ClientRectangle = new Rectangle(
            lblMapName.X,
            lblMapName.Y + 18,
            0,
            0);
        LblMapNameValue.Text = "Map name".L10N("UI:Main:MapName");

        lblGameMode = new XNALabel(WindowManager);
        lblGameMode.Name = nameof(lblGameMode);
        lblGameMode.ClientRectangle = new Rectangle(
            lblMapName.X,
            PanelPlayers.Y + 40,
            0,
            0);
        lblGameMode.FontIndex = 1;
        lblGameMode.Text = "GAME MODE:".L10N("UI:Main:GameMode");

        LblGameModeValue = new XNALabel(WindowManager);
        LblGameModeValue.Name = nameof(LblGameModeValue);
        LblGameModeValue.ClientRectangle = new Rectangle(
            lblGameMode.X,
            lblGameMode.Y + 18,
            0,
            0);
        LblGameModeValue.Text = "Game mode".L10N("UI:Main:GameModeValueText");

        lblSavedGameTime = new XNALabel(WindowManager);
        lblSavedGameTime.Name = nameof(lblSavedGameTime);
        lblSavedGameTime.ClientRectangle = new Rectangle(
            lblMapName.X,
            PanelPlayers.Bottom - 40,
            0,
            0);
        lblSavedGameTime.FontIndex = 1;
        lblSavedGameTime.Text = "SAVED GAME:".L10N("UI:Main:SavedGame");

        DdSavedGame = new XNAClientDropDown(WindowManager);
        DdSavedGame.Name = nameof(DdSavedGame);
        DdSavedGame.ClientRectangle = new Rectangle(
            lblSavedGameTime.X,
            PanelPlayers.Bottom - 21,
            Width - lblSavedGameTime.X - 12,
            21);
        DdSavedGame.SelectedIndexChanged += DdSavedGame_SelectedIndexChanged;

        LbChatMessages = new ChatListBox(WindowManager);
        LbChatMessages.Name = nameof(LbChatMessages);
        LbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
        LbChatMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        LbChatMessages.ClientRectangle = new Rectangle(
            12,
            PanelPlayers.Bottom + 12,
            Width - 24,
            Height - PanelPlayers.Bottom - 12 - 29 - 34);

        TbChatInput = new XNATextBox(WindowManager);
        TbChatInput.Name = nameof(TbChatInput);
        TbChatInput.ClientRectangle = new Rectangle(
            LbChatMessages.X,
            LbChatMessages.Bottom + 3,
            LbChatMessages.Width,
            19);
        TbChatInput.MaximumTextLength = 200;
        TbChatInput.EnterPressed += TbChatInput_EnterPressed;

        BtnLoadGame = new XNAClientButton(WindowManager);
        BtnLoadGame.Name = nameof(BtnLoadGame);
        BtnLoadGame.ClientRectangle = new Rectangle(
            LbChatMessages.X,
            TbChatInput.Bottom + 6,
            UIDesignConstants.ButtonWidth133,
            UIDesignConstants.ButtonHeight);
        BtnLoadGame.Text = "Load Game".L10N("UI:Main:LoadGame");
        BtnLoadGame.LeftClick += BtnLoadGame_LeftClick;

        BtnLeaveGame = new XNAClientButton(WindowManager);
        BtnLeaveGame.Name = nameof(BtnLeaveGame);
        BtnLeaveGame.ClientRectangle = new Rectangle(
            Width - 145,
            BtnLoadGame.Y,
            UIDesignConstants.ButtonWidth133,
            UIDesignConstants.ButtonHeight);
        BtnLeaveGame.Text = "Leave Game".L10N("UI:Main:LeaveGame");
        BtnLeaveGame.LeftClick += BtnLeaveGame_LeftClick;

        AddChild(lblMapName);
        AddChild(LblMapNameValue);
        AddChild(lblGameMode);
        AddChild(LblGameModeValue);
        AddChild(lblSavedGameTime);
        AddChild(LbChatMessages);
        AddChild(TbChatInput);
        AddChild(BtnLoadGame);
        AddChild(BtnLeaveGame);
        AddChild(DdSavedGame);

        base.Initialize();

        SndJoinSound = new EnhancedSoundEffect("joingame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyJoinCooldown);
        SndLeaveSound = new EnhancedSoundEffect("leavegame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyLeaveCooldown);
        SndMessageSound = new EnhancedSoundEffect("message.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundMessageCooldown);
        SndGetReadySound = new EnhancedSoundEffect("getready.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyGetReadyCooldown);

        mPColors = MultiplayerColor.LoadColors();

        WindowManager.CenterControlOnScreen(this);

        if (SavedGameManager.AreSavedGamesAvailable())
        {
            fsw = new FileSystemWatcher(ProgramConstants.GamePath + "Saved Games", "*.NET")
            {
                EnableRaisingEvents = false
            };
            fsw.Created += Fsw_Created;
            fsw.Changed += Fsw_Created;
        }
    }

    /// <summary>
    /// Refreshes the UI based on the latest saved game and information in the saved spawn.ini file,
    /// as well as information on whether the local player is the host of the game.
    /// </summary>
    /// <param name="isHost">is host.</param>
    public void Refresh(bool isHost)
    {
        isSettingUp = true;
        IsHost = isHost;

        SGPlayers.Clear();
        Players.Clear();
        DdSavedGame.Items.Clear();
        LbChatMessages.Clear();
        LbChatMessages.TopIndex = 0;

        DdSavedGame.AllowDropDown = isHost;
        BtnLoadGame.Text = isHost ? "Load Game".L10N("UI:Main:ButtonLoadGame") : "I'm Ready".L10N("UI:Main:ButtonGetReady");

        IniFile spawnSGIni = new(ProgramConstants.GamePath + "Saved Games/spawnSG.ini");

        //loadedGameID = spawnSGIni.GetStringValue("Settings", "GameID", "0");
        LblMapNameValue.Text = spawnSGIni.GetStringValue("Settings", "UIMapName", string.Empty);
        LblGameModeValue.Text = spawnSGIni.GetStringValue("Settings", "UIGameMode", string.Empty);

        uniqueGameId = spawnSGIni.GetIntValue("Settings", "GameID", -1);

        int playerCount = spawnSGIni.GetIntValue("Settings", "PlayerCount", 0);

        SavedGamePlayer localPlayer = new()
        {
            Name = ProgramConstants.PLAYERNAME,
            ColorIndex = mPColors.FindIndex(
            c => c.GameColorIndex == spawnSGIni.GetIntValue("Settings", "Color", 0))
        };

        SGPlayers.Add(localPlayer);

        for (int i = 1; i < playerCount; i++)
        {
            string sectionName = "Other" + i;

            SavedGamePlayer sgPlayer = new()
            {
                Name = spawnSGIni.GetStringValue(sectionName, "Name", "Unknown player".L10N("UI:Main:UnknownPlayer")),
                ColorIndex = mPColors.FindIndex(
                c => c.GameColorIndex == spawnSGIni.GetIntValue(sectionName, "Color", 0))
            };

            SGPlayers.Add(sgPlayer);
        }

        for (int i = 0; i < SGPlayers.Count; i++)
        {
            LblPlayerNames[i].Enabled = true;
            LblPlayerNames[i].Visible = true;
        }

        for (int i = SGPlayers.Count; i < 8; i++)
        {
            LblPlayerNames[i].Enabled = false;
            LblPlayerNames[i].Visible = false;
        }

        List<string> timestamps = SavedGameManager.GetSaveGameTimestamps();
        timestamps.Reverse(); // Most recent saved game first

        timestamps.ForEach(DdSavedGame.AddItem);

        if (DdSavedGame.Items.Count > 0)
            DdSavedGame.SelectedIndex = 0;

        CopyPlayerDataToUI();
        isSettingUp = false;
    }

    public void SwitchOff() => Disable();

    public void SwitchOn() => Enable();

    protected void AddNotice(string notice) => AddNotice(notice, Color.White);

    protected abstract void AddNotice(string message, Color color);

    /// <summary>
    /// Override in a derived class to broadcast player ready statuses and the selected saved game
    /// to players.
    /// </summary>
    protected abstract void BroadcastOptions();

    protected void CopyPlayerDataToUI()
    {
        for (int i = 0; i < SGPlayers.Count; i++)
        {
            SavedGamePlayer sgPlayer = SGPlayers[i];

            PlayerInfo pInfo = Players.Find(p => p.Name == SGPlayers[i].Name);

            XNALabel playerLabel = LblPlayerNames[i];

            if (pInfo == null)
            {
                playerLabel.RemapColor = Color.Gray;
                playerLabel.Text = sgPlayer.Name + " " + "(Not present)".L10N("UI:Main:NotPresentSuffix");
                continue;
            }

            playerLabel.RemapColor = sgPlayer.ColorIndex > -1 ? mPColors[sgPlayer.ColorIndex].XnaColor
                : Color.White;
            playerLabel.Text = pInfo.Ready ? sgPlayer.Name : sgPlayer.Name + " " + "(Not Ready)".L10N("UI:Main:NotReadySuffix");
        }
    }

    protected virtual string GetIPAddressForPlayer(PlayerInfo pInfo) => "0.0.0.0";

    protected virtual void GetReadyNotification()
    {
        AddNotice("The game host wants to load the game but cannot because not all players are ready!".L10N("UI:Main:GetReadyPlease"));

        if (!IsHost && !Players.Find(p => p.Name == ProgramConstants.PLAYERNAME).Ready)
            SndGetReadySound.Play();

        WindowManager.FlashWindow();
    }

    protected virtual void HandleGameProcessExited()
    {
        fsw.EnableRaisingEvents = false;

        GameProcessLogic.GameProcessExited -= SharedUILogic_GameProcessExited;

        MatchStatistics matchStatistics = StatisticsManager.Instance.GetMatchWithGameID(uniqueGameId);

        if (matchStatistics != null)
        {
            int oldLength = matchStatistics.LengthInSeconds;
            int newLength = matchStatistics.LengthInSeconds +
                (int)(DateTime.Now - gameLoadTime).TotalSeconds;

            matchStatistics.ParseStatistics(
                ProgramConstants.GamePath,
                true);

            matchStatistics.LengthInSeconds = newLength;

            StatisticsManager.Instance.SaveDatabase();
        }

        UpdateDiscordPresence(true);
    }

    protected abstract void HostStartGame();

    protected virtual void LeaveGame()
    {
        GameLeft?.Invoke(this, EventArgs.Empty);
        ResetDiscordPresence();
    }

    protected void LoadGame()
    {
        File.Delete(ProgramConstants.GamePath + "spawn.ini");

        File.Copy(ProgramConstants.GamePath + "Saved Games/spawnSG.ini", ProgramConstants.GamePath + "spawn.ini");

        IniFile spawnIni = new(ProgramConstants.GamePath + "spawn.ini");

        int sgIndex = DdSavedGame.Items.Count - 1 - DdSavedGame.SelectedIndex;

        spawnIni.SetStringValue(
            "Settings",
            "SaveGameName",
            string.Format("SVGM_{0}.NET", sgIndex.ToString("D3")));
        spawnIni.SetBooleanValue("Settings", "LoadSaveGame", true);

        PlayerInfo localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

        if (localPlayer == null)
            return;

        spawnIni.SetIntValue("Settings", "Port", localPlayer.Port);

        for (int i = 1; i < Players.Count; i++)
        {
            string otherName = spawnIni.GetStringValue("Other" + i, "Name", string.Empty);

            if (string.IsNullOrEmpty(otherName))
                continue;

            PlayerInfo otherPlayer = Players.Find(p => p.Name == otherName);

            if (otherPlayer == null)
                continue;

            spawnIni.SetStringValue("Other" + i, "Ip", otherPlayer.IPAddress);
            spawnIni.SetIntValue("Other" + i, "Port", otherPlayer.Port);
        }

        WriteSpawnIniAdditions(spawnIni);
        spawnIni.WriteIniFile();

        File.Delete(ProgramConstants.GamePath + "spawnmap.ini");
        StreamWriter sw = new(ProgramConstants.GamePath + "spawnmap.ini");
        sw.WriteLine("[Map]");
        sw.WriteLine("Size=0,0,50,50");
        sw.WriteLine("LocalSize=0,0,50,50");
        sw.WriteLine();
        sw.Close();

        gameLoadTime = DateTime.Now;

        GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;
        GameProcessLogic.StartGameProcess();

        fsw.EnableRaisingEvents = true;
        UpdateDiscordPresence(true);
    }

    protected virtual void NotAllPresentNotification() =>
            AddNotice("You cannot load the game before all players are present.".L10N("UI:Main:NotAllPresent"));

    protected abstract void RequestReadyStatus();

    /// <summary>
    /// Resets Discord Rich Presence to default state.
    /// </summary>
    protected void ResetDiscordPresence() => DiscordHandler?.UpdatePresence();

    protected abstract void SendChatMessage(string message);

    /// <summary>
    /// Updates Discord Rich Presence with actual information.
    /// </summary>
    /// <param name="resetTimer">Whether to restart the "Elapsed" timer or not.</param>
    protected abstract void UpdateDiscordPresence(bool resetTimer = false);

    protected virtual void WriteSpawnIniAdditions(IniFile spawnIni)
    {
        // Do nothing by default
    }

    private void BtnLeaveGame_LeftClick(object sender, EventArgs e) => LeaveGame();

    private void BtnLoadGame_LeftClick(object sender, EventArgs e)
    {
        if (!IsHost)
        {
            RequestReadyStatus();
            return;
        }

        if (Players.Find(p => !p.Ready) != null)
        {
            GetReadyNotification();
            return;
        }

        if (Players.Count != SGPlayers.Count)
        {
            NotAllPresentNotification();
            return;
        }

        HostStartGame();
    }

    private void DdSavedGame_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!IsHost)
            return;

        for (int i = 1; i < Players.Count; i++)
            Players[i].Ready = false;

        CopyPlayerDataToUI();

        if (!isSettingUp)
            BroadcastOptions();
        UpdateDiscordPresence();
    }

    private void Fsw_Created(object sender, FileSystemEventArgs e) =>
                AddCallback(new Action<FileSystemEventArgs>(HandleFSWEvent), e);

    private void HandleFSWEvent(FileSystemEventArgs e)
    {
        Logger.Log("FSW Event: " + e.FullPath);

        if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
        {
            SavedGameManager.RenameSavedGame();
        }
    }

    private void SharedUILogic_GameProcessExited() =>
        AddCallback(new Action(HandleGameProcessExited), null);

    private void TbChatInput_EnterPressed(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(TbChatInput.Text))
            return;

        SendChatMessage(TbChatInput.Text);
        TbChatInput.Text = string.Empty;
    }
}