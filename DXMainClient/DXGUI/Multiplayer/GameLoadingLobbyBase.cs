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
    /// <summary>
    /// The list of players in the current saved game.
    /// </summary>
    protected List<SavedGamePlayer> sGPlayers = new();

    public GameLoadingLobbyBase(WindowManager windowManager, DiscordHandler discordHandler)
        : base(windowManager)
    {
        this.discordHandler = discordHandler;
    }

    public event EventHandler GameLeft;

    /// <summary>
    /// The list of players in the game lobby.
    /// </summary>
    protected List<PlayerInfo> players = new();

    protected bool isHost = false;

    protected DiscordHandler discordHandler;

    protected XNAClientDropDown ddSavedGame;

    protected ChatListBox lbChatMessages;
    protected XNATextBox tbChatInput;

    protected EnhancedSoundEffect sndGetReadySound;
    protected EnhancedSoundEffect sndJoinSound;
    protected EnhancedSoundEffect sndLeaveSound;
    protected EnhancedSoundEffect sndMessageSound;

    protected XNALabel lblDescription;
    protected XNAPanel panelPlayers;
    protected XNALabel[] lblPlayerNames;
    protected XNALabel lblMapNameValue;
    protected XNALabel lblGameModeValue;

    protected XNAClientButton btnLoadGame;

    private XNALabel lblMapName;
    private XNALabel lblGameMode;
    private XNALabel lblSavedGameTime;
    protected XNAClientButton btnLeaveGame;

    private List<MultiplayerColor> mPColors = new();

    private string loadedGameID;

    private bool isSettingUp = false;
    private FileSystemWatcher fsw;

    private int uniqueGameId = 0;
    private DateTime gameLoadTime;

    public override void Initialize()
    {
        Name = "GameLoadingLobby";
        ClientRectangle = new Rectangle(0, 0, 590, 510);
        BackgroundTexture = AssetLoader.LoadTexture("loadmpsavebg.png");

        lblDescription = new XNALabel(WindowManager);
        lblDescription.Name = nameof(lblDescription);
        lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
        lblDescription.Text = "Wait for all players to join and get ready, then click Load Game to load the saved multiplayer game.".L10N("UI:Main:LobbyInitialTip");

        panelPlayers = new XNAPanel(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 32, 373, 125),
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1),
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED
        };

        AddChild(lblDescription);
        AddChild(panelPlayers);

        lblPlayerNames = new XNALabel[8];
        for (int i = 0; i < 8; i++)
        {
            XNALabel lblPlayerName = new(WindowManager);
            lblPlayerName.Name = nameof(lblPlayerName) + i;

            lblPlayerName.ClientRectangle = i < 4 ? new Rectangle(9, 9 + (30 * i), 0, 0) : new Rectangle(190, 9 + (30 * (i - 4)), 0, 0);

            lblPlayerName.Text = string.Format("Player {0}".L10N("UI:Main:PlayerX"), i) + " ";
            panelPlayers.AddChild(lblPlayerName);
            lblPlayerNames[i] = lblPlayerName;
        }

        lblMapName = new XNALabel(WindowManager);
        lblMapName.Name = nameof(lblMapName);
        lblMapName.FontIndex = 1;
        lblMapName.ClientRectangle = new Rectangle(
            panelPlayers.Right + 12,
            panelPlayers.Y, 0, 0);
        lblMapName.Text = "MAP:".L10N("UI:Main:MapLabel");

        lblMapNameValue = new XNALabel(WindowManager);
        lblMapNameValue.Name = nameof(lblMapNameValue);
        lblMapNameValue.ClientRectangle = new Rectangle(
            lblMapName.X,
            lblMapName.Y + 18, 0, 0);
        lblMapNameValue.Text = "Map name".L10N("UI:Main:MapName");

        lblGameMode = new XNALabel(WindowManager);
        lblGameMode.Name = nameof(lblGameMode);
        lblGameMode.ClientRectangle = new Rectangle(
            lblMapName.X,
            panelPlayers.Y + 40, 0, 0);
        lblGameMode.FontIndex = 1;
        lblGameMode.Text = "GAME MODE:".L10N("UI:Main:GameMode");

        lblGameModeValue = new XNALabel(WindowManager);
        lblGameModeValue.Name = nameof(lblGameModeValue);
        lblGameModeValue.ClientRectangle = new Rectangle(
            lblGameMode.X,
            lblGameMode.Y + 18, 0, 0);
        lblGameModeValue.Text = "Game mode".L10N("UI:Main:GameModeValueText");

        lblSavedGameTime = new XNALabel(WindowManager);
        lblSavedGameTime.Name = nameof(lblSavedGameTime);
        lblSavedGameTime.ClientRectangle = new Rectangle(
            lblMapName.X,
            panelPlayers.Bottom - 40, 0, 0);
        lblSavedGameTime.FontIndex = 1;
        lblSavedGameTime.Text = "SAVED GAME:".L10N("UI:Main:SavedGame");

        ddSavedGame = new XNAClientDropDown(WindowManager);
        ddSavedGame.Name = nameof(ddSavedGame);
        ddSavedGame.ClientRectangle = new Rectangle(
            lblSavedGameTime.X,
            panelPlayers.Bottom - 21,
            Width - lblSavedGameTime.X - 12, 21);
        ddSavedGame.SelectedIndexChanged += DdSavedGame_SelectedIndexChanged;

        lbChatMessages = new ChatListBox(WindowManager);
        lbChatMessages.Name = nameof(lbChatMessages);
        lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
        lbChatMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        lbChatMessages.ClientRectangle = new Rectangle(12, panelPlayers.Bottom + 12,
            Width - 24,
            Height - panelPlayers.Bottom - 12 - 29 - 34);

        tbChatInput = new XNATextBox(WindowManager);
        tbChatInput.Name = nameof(tbChatInput);
        tbChatInput.ClientRectangle = new Rectangle(
            lbChatMessages.X,
            lbChatMessages.Bottom + 3, lbChatMessages.Width, 19);
        tbChatInput.MaximumTextLength = 200;
        tbChatInput.EnterPressed += TbChatInput_EnterPressed;

        btnLoadGame = new XNAClientButton(WindowManager);
        btnLoadGame.Name = nameof(btnLoadGame);
        btnLoadGame.ClientRectangle = new Rectangle(
            lbChatMessages.X,
            tbChatInput.Bottom + 6, UIDesignConstants.BUTTONWIDTH133, UIDesignConstants.BUTTONHEIGHT);
        btnLoadGame.Text = "Load Game".L10N("UI:Main:LoadGame");
        btnLoadGame.LeftClick += BtnLoadGame_LeftClick;

        btnLeaveGame = new XNAClientButton(WindowManager);
        btnLeaveGame.Name = nameof(btnLeaveGame);
        btnLeaveGame.ClientRectangle = new Rectangle(
            Width - 145,
            btnLoadGame.Y, UIDesignConstants.BUTTONWIDTH133, UIDesignConstants.BUTTONHEIGHT);
        btnLeaveGame.Text = "Leave Game".L10N("UI:Main:LeaveGame");
        btnLeaveGame.LeftClick += BtnLeaveGame_LeftClick;

        AddChild(lblMapName);
        AddChild(lblMapNameValue);
        AddChild(lblGameMode);
        AddChild(lblGameModeValue);
        AddChild(lblSavedGameTime);
        AddChild(lbChatMessages);
        AddChild(tbChatInput);
        AddChild(btnLoadGame);
        AddChild(btnLeaveGame);
        AddChild(ddSavedGame);

        base.Initialize();

        sndJoinSound = new EnhancedSoundEffect("joingame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyJoinCooldown);
        sndLeaveSound = new EnhancedSoundEffect("leavegame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyLeaveCooldown);
        sndMessageSound = new EnhancedSoundEffect("message.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundMessageCooldown);
        sndGetReadySound = new EnhancedSoundEffect("getready.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyGetReadyCooldown);

        mPColors = MultiplayerColor.LoadColors();

        WindowManager.CenterControlOnScreen(this);

        if (SavedGameManager.AreSavedGamesAvailable())
        {
            fsw = new FileSystemWatcher(ProgramConstants.GamePath + "Saved Games", "*.NET")
            {
                EnableRaisingEvents = false
            };
            fsw.Created += fsw_Created;
            fsw.Changed += fsw_Created;
        }
    }

    /// <summary>
    /// Refreshes the UI  based on the latest saved game
    /// and information in the saved spawn.ini file, as well
    /// as information on whether the local player is the host of the game.
    /// </summary>
    public void Refresh(bool isHost)
    {
        isSettingUp = true;
        this.isHost = isHost;

        sGPlayers.Clear();
        players.Clear();
        ddSavedGame.Items.Clear();
        lbChatMessages.Clear();
        lbChatMessages.TopIndex = 0;

        ddSavedGame.AllowDropDown = isHost;
        btnLoadGame.Text = isHost ? "Load Game".L10N("UI:Main:ButtonLoadGame") : "I'm Ready".L10N("UI:Main:ButtonGetReady");

        IniFile spawnSGIni = new(ProgramConstants.GamePath + "Saved Games/spawnSG.ini");

        loadedGameID = spawnSGIni.GetStringValue("Settings", "GameID", "0");
        lblMapNameValue.Text = spawnSGIni.GetStringValue("Settings", "UIMapName", string.Empty);
        lblGameModeValue.Text = spawnSGIni.GetStringValue("Settings", "UIGameMode", string.Empty);

        uniqueGameId = spawnSGIni.GetIntValue("Settings", "GameID", -1);

        int playerCount = spawnSGIni.GetIntValue("Settings", "PlayerCount", 0);

        SavedGamePlayer localPlayer = new()
        {
            Name = ProgramConstants.PLAYERNAME,
            ColorIndex = mPColors.FindIndex(
            c => c.GameColorIndex == spawnSGIni.GetIntValue("Settings", "Color", 0))
        };

        sGPlayers.Add(localPlayer);

        for (int i = 1; i < playerCount; i++)
        {
            string sectionName = "Other" + i;

            SavedGamePlayer sgPlayer = new()
            {
                Name = spawnSGIni.GetStringValue(sectionName, "Name", "Unknown player".L10N("UI:Main:UnknownPlayer")),
                ColorIndex = mPColors.FindIndex(
                c => c.GameColorIndex == spawnSGIni.GetIntValue(sectionName, "Color", 0))
            };

            sGPlayers.Add(sgPlayer);
        }

        for (int i = 0; i < sGPlayers.Count; i++)
        {
            lblPlayerNames[i].Enabled = true;
            lblPlayerNames[i].Visible = true;
        }

        for (int i = sGPlayers.Count; i < 8; i++)
        {
            lblPlayerNames[i].Enabled = false;
            lblPlayerNames[i].Visible = false;
        }

        List<string> timestamps = SavedGameManager.GetSaveGameTimestamps();
        timestamps.Reverse(); // Most recent saved game first

        timestamps.ForEach(ddSavedGame.AddItem);

        if (ddSavedGame.Items.Count > 0)
            ddSavedGame.SelectedIndex = 0;

        CopyPlayerDataToUI();
        isSettingUp = false;
    }

    public override void Draw(GameTime gameTime)
    {
        Renderer.FillRectangle(
            new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY),
            new Color(0, 0, 0, 255));

        base.Draw(gameTime);
    }

    /// <summary>
    /// Updates Discord Rich Presence with actual information.
    /// </summary>
    /// <param name="resetTimer">Whether to restart the "Elapsed" timer or not.</param>
    protected abstract void UpdateDiscordPresence(bool resetTimer = false);

    /// <summary>
    /// Resets Discord Rich Presence to default state.
    /// </summary>
    protected void ResetDiscordPresence() => discordHandler?.UpdatePresence();

    protected virtual void LeaveGame()
    {
        GameLeft?.Invoke(this, EventArgs.Empty);
        ResetDiscordPresence();
    }

    protected abstract void RequestReadyStatus();

    private void BtnLeaveGame_LeftClick(object sender, EventArgs e) => LeaveGame();

    private void fsw_Created(object sender, FileSystemEventArgs e) =>
        AddCallback(new Action<FileSystemEventArgs>(HandleFSWEvent), e);

    private void HandleFSWEvent(FileSystemEventArgs e)
    {
        Logger.Log("FSW Event: " + e.FullPath);

        if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
        {
            SavedGameManager.RenameSavedGame();
        }
    }

    private void BtnLoadGame_LeftClick(object sender, EventArgs e)
    {
        if (!isHost)
        {
            RequestReadyStatus();
            return;
        }

        if (players.Find(p => !p.Ready) != null)
        {
            GetReadyNotification();
            return;
        }

        if (players.Count != sGPlayers.Count)
        {
            NotAllPresentNotification();
            return;
        }

        HostStartGame();
    }

    protected virtual void GetReadyNotification()
    {
        AddNotice("The game host wants to load the game but cannot because not all players are ready!".L10N("UI:Main:GetReadyPlease"));

        if (!isHost && !players.Find(p => p.Name == ProgramConstants.PLAYERNAME).Ready)
            sndGetReadySound.Play();

        WindowManager.FlashWindow();
    }

    protected virtual void NotAllPresentNotification() =>
        AddNotice("You cannot load the game before all players are present.".L10N("UI:Main:NotAllPresent"));

    protected abstract void HostStartGame();

    protected void LoadGame()
    {
        File.Delete(ProgramConstants.GamePath + "spawn.ini");

        File.Copy(ProgramConstants.GamePath + "Saved Games/spawnSG.ini", ProgramConstants.GamePath + "spawn.ini");

        IniFile spawnIni = new(ProgramConstants.GamePath + "spawn.ini");

        int sgIndex = ddSavedGame.Items.Count - 1 - ddSavedGame.SelectedIndex;

        spawnIni.SetStringValue("Settings", "SaveGameName",
            string.Format("SVGM_{0}.NET", sgIndex.ToString("D3")));
        spawnIni.SetBooleanValue("Settings", "LoadSaveGame", true);

        PlayerInfo localPlayer = players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

        if (localPlayer == null)
            return;

        spawnIni.SetIntValue("Settings", "Port", localPlayer.Port);

        for (int i = 1; i < players.Count; i++)
        {
            string otherName = spawnIni.GetStringValue("Other" + i, "Name", string.Empty);

            if (string.IsNullOrEmpty(otherName))
                continue;

            PlayerInfo otherPlayer = players.Find(p => p.Name == otherName);

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
                ClientConfiguration.Instance.LocalGame, true);

            matchStatistics.LengthInSeconds = newLength;

            StatisticsManager.Instance.SaveDatabase();
        }

        UpdateDiscordPresence(true);
    }

    private void SharedUILogic_GameProcessExited() =>
        AddCallback(new Action(HandleGameProcessExited), null);

    protected virtual void WriteSpawnIniAdditions(IniFile spawnIni)
    {
        // Do nothing by default
    }

    protected void AddNotice(string notice) => AddNotice(notice, Color.White);

    protected abstract void AddNotice(string message, Color color);

    protected void CopyPlayerDataToUI()
    {
        for (int i = 0; i < sGPlayers.Count; i++)
        {
            SavedGamePlayer sgPlayer = sGPlayers[i];

            PlayerInfo pInfo = players.Find(p => p.Name == sGPlayers[i].Name);

            XNALabel playerLabel = lblPlayerNames[i];

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

    /// <summary>
    /// Override in a derived class to broadcast player ready statuses and the selected
    /// saved game to players.
    /// </summary>
    protected abstract void BroadcastOptions();

    private void DdSavedGame_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!isHost)
            return;

        for (int i = 1; i < players.Count; i++)
            players[i].Ready = false;

        CopyPlayerDataToUI();

        if (!isSettingUp)
            BroadcastOptions();
        UpdateDiscordPresence();
    }

    private void TbChatInput_EnterPressed(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(tbChatInput.Text))
            return;

        SendChatMessage(tbChatInput.Text);
        tbChatInput.Text = string.Empty;
    }

    protected abstract void SendChatMessage(string message);

    public void SwitchOn() => Enable();

    public void SwitchOff() => Disable();

    public abstract string GetSwitchName();
}