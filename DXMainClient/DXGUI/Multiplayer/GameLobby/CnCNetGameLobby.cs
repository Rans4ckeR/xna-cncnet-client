using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class CnCNetGameLobby : MultiplayerGameLobby
{
    private const int HUMAN_PLAYER_OPTIONS_LENGTH = 3;
    private const int AI_PLAYER_OPTIONS_LENGTH = 2;

    private const double GAME_BROADCAST_INTERVAL = 30.0;
    private const double GAME_BROADCAST_ACCELERATION = 10.0;
    private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;

    private const string MAP_SHARING_FAIL_MESSAGE = "MAPFAIL";

    private static readonly Color ERROR_MESSAGE_COLOR = Color.Yellow;
    private const string MAP_SHARING_DOWNLOAD_REQUEST = "MAPOK";
    private const string MAP_SHARING_UPLOAD_REQUEST = "MAPREQ";
    private const string MAP_SHARING_DISABLED_MESSAGE = "MAPSDISABLED";
    private const string CHEAT_DETECTED_MESSAGE = "CD";
    private const string DICE_ROLL_MESSAGE = "DR";
    private const string CHANGE_TUNNEL_SERVER_MESSAGE = "CHTNL";

    private readonly TunnelHandler tunnelHandler;
    private readonly CnCNetManager connectionManager;

    public CnCNetGameLobby(WindowManager windowManager, string iniName,
        TopBar topBar, CnCNetManager connectionManager,
        TunnelHandler tunnelHandler, GameCollection gameCollection, CnCNetUserData cncnetUserData, MapLoader mapLoader, DiscordHandler discordHandler,
        PrivateMessagingWindow pmWindow)
        : base(windowManager, iniName, topBar, mapLoader, discordHandler)
    {
        this.connectionManager = connectionManager;
        localGame = ClientConfiguration.Instance.LocalGame;
        this.tunnelHandler = tunnelHandler;
        this.gameCollection = gameCollection;
        this.cncnetUserData = cncnetUserData;
        this.pmWindow = pmWindow;

        ctcpCommandHandlers = new CommandHandlerBase[]
        {
            new IntCommandHandler("OR", HandleOptionsRequest),
            new IntCommandHandler("R", HandleReadyRequest),
            new StringCommandHandler("PO", ApplyPlayerOptions),
            new StringCommandHandler(PlayerExtraOptions.CNCNETMESSAGEKEY, ApplyPlayerExtraOptions),
            new StringCommandHandler("GO", ApplyGameOptions),
            new StringCommandHandler("START", NonHostLaunchGame),
            new NotificationHandler("AISPECS", HandleNotification, AISpectatorsNotification),
            new NotificationHandler("GETREADY", HandleNotification, GetReadyNotification),
            new NotificationHandler("INSFSPLRS", HandleNotification, InsufficientPlayersNotification),
            new NotificationHandler("TMPLRS", HandleNotification, TooManyPlayersNotification),
            new NotificationHandler("CLRS", HandleNotification, SharedColorsNotification),
            new NotificationHandler("SLOC", HandleNotification, SharedStartingLocationNotification),
            new NotificationHandler("LCKGME", HandleNotification, LockGameNotification),
            new IntNotificationHandler("NVRFY", HandleIntNotification, NotVerifiedNotification),
            new IntNotificationHandler("INGM", HandleIntNotification, StillInGameNotification),
            new StringCommandHandler(MAP_SHARING_UPLOAD_REQUEST, HandleMapUploadRequest),
            new StringCommandHandler(MAP_SHARING_FAIL_MESSAGE, HandleMapTransferFailMessage),
            new StringCommandHandler(MAP_SHARING_DOWNLOAD_REQUEST, HandleMapDownloadRequest),
            new NoParamCommandHandler(MAP_SHARING_DISABLED_MESSAGE, HandleMapSharingBlockedMessage),
            new NoParamCommandHandler("RETURN", ReturnNotification),
            new IntCommandHandler("TNLPNG", HandleTunnelPing),
            new StringCommandHandler("FHSH", FileHashNotification),
            new StringCommandHandler("MM", CheaterNotification),
            new StringCommandHandler(DICE_ROLL_MESSAGE, HandleDiceRollResult),
            new NoParamCommandHandler(CHEAT_DETECTED_MESSAGE, HandleCheatDetectedMessage),
            new StringCommandHandler(CHANGE_TUNNEL_SERVER_MESSAGE, HandleTunnelServerChangeMessage)
        };

        MapSharer.MapDownloadFailed += MapSharer_MapDownloadFailed;
        MapSharer.MapDownloadComplete += MapSharer_MapDownloadComplete;
        MapSharer.MapUploadFailed += MapSharer_MapUploadFailed;
        MapSharer.MapUploadComplete += MapSharer_MapUploadComplete;

        AddChatBoxCommand(new ChatBoxCommand(
            "TUNNELINFO",
            "View tunnel server information".L10N("UI:Main:CommandTunnelInfo"), false, PrintTunnelServerInformation));
        AddChatBoxCommand(new ChatBoxCommand(
            "CHANGETUNNEL",
            "Change the used CnCNet tunnel server (game host only)".L10N("UI:Main:CommandChangeTunnel"),
            true, (s) => ShowTunnelSelectionWindow("Select tunnel server:".L10N("UI:Main:SelectTunnelServer"))));
    }

    public event EventHandler GameLeft;
    private TunnelSelectionWindow tunnelSelectionWindow;
    private XNAClientButton btnChangeTunnel;

    private Channel channel;
    private readonly string localGame;

    private readonly GameCollection gameCollection;
    private readonly CnCNetUserData cncnetUserData;
    private readonly PrivateMessagingWindow pmWindow;
    private readonly CommandHandlerBase[] ctcpCommandHandlers;

    private readonly List<string> hostUploadedMaps = new();

    private GlobalContextMenu globalContextMenu;

    private string hostName;

    private IRCColor chatColor;

    private XNATimerControl gameBroadcastTimer;

    private int playerLimit;

    private bool closed = false;

    private bool isCustomPassword = false;

    private string gameFilesHash;

    private MapSharingConfirmationPanel mapSharingConfirmationPanel;

    /// <summary>
    /// The SHA1 of the latest selected map.
    /// Used for map sharing.
    /// </summary>
    private string lastMapSHA1;

    /// <summary>
    /// The map name of the latest selected map.
    /// Used for map sharing.
    /// </summary>
    private string lastMapName;

    /// <summary>
    /// The game mode of the latest selected map.
    /// Used for map sharing.
    /// </summary>
    private string lastGameMode;

    public override void Initialize()
    {
        IniNameOverride = nameof(CnCNetGameLobby);
        base.Initialize();

        btnChangeTunnel = FindChild<XNAClientButton>(nameof(btnChangeTunnel));
        btnChangeTunnel.LeftClick += BtnChangeTunnel_LeftClick;

        gameBroadcastTimer = new XNATimerControl(WindowManager)
        {
            AutoReset = true,
            Interval = TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL),
            Enabled = false
        };
        gameBroadcastTimer.TimeElapsed += GameBroadcastTimer_TimeElapsed;

        tunnelSelectionWindow = new TunnelSelectionWindow(WindowManager, tunnelHandler);
        tunnelSelectionWindow.Initialize();
        tunnelSelectionWindow.DrawOrder = 1;
        tunnelSelectionWindow.UpdateOrder = 1;
        DarkeningPanel.AddAndInitializeWithControl(WindowManager, tunnelSelectionWindow);
        tunnelSelectionWindow.CenterOnParent();
        tunnelSelectionWindow.Disable();
        tunnelSelectionWindow.TunnelSelected += TunnelSelectionWindow_TunnelSelected;

        mapSharingConfirmationPanel = new MapSharingConfirmationPanel(WindowManager);
        mapPreviewBox.AddChild(mapSharingConfirmationPanel);
        mapSharingConfirmationPanel.MapDownloadConfirmed += MapSharingConfirmationPanel_MapDownloadConfirmed;

        WindowManager.AddAndInitializeControl(gameBroadcastTimer);

        globalContextMenu = new GlobalContextMenu(WindowManager, connectionManager, cncnetUserData, pmWindow);
        AddChild(globalContextMenu);

        multiplayerNameRightClicked += MultiplayerName_RightClick;

        PostInitialize();
    }

    public void SetUp(Channel channel, bool isHost, int playerLimit,
        CnCNetTunnel tunnel, string hostName, bool isCustomPassword)
    {
        this.channel = channel;
        channel.MessageAdded += Channel_MessageAdded;
        channel.CTCPReceived += Channel_CTCPReceived;
        channel.UserKicked += Channel_UserKicked;
        channel.UserQuitIRC += Channel_UserQuitIRC;
        channel.UserLeft += Channel_UserLeft;
        channel.UserAdded += Channel_UserAdded;
        channel.UserNameChanged += Channel_UserNameChanged;
        channel.UserListReceived += Channel_UserListReceived;

        this.hostName = hostName;
        this.playerLimit = playerLimit;
        this.isCustomPassword = isCustomPassword;

        if (isHost)
        {
            RandomSeed = new Random().Next();
            RefreshMapSelectionUI();
            btnChangeTunnel.Enable();
        }
        else
        {
            channel.ChannelModesChanged += Channel_ChannelModesChanged;
            aIPlayers.Clear();
            btnChangeTunnel.Disable();
        }

        tunnelHandler.CurrentTunnel = tunnel;
        tunnelHandler.CurrentTunnelPinged += TunnelHandler_CurrentTunnelPinged;

        connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
        connectionManager.Disconnected += ConnectionManager_Disconnected;

        Refresh(isHost);
    }

    public void OnJoined()
    {
        FileHashCalculator fhc = new();
        fhc.CalculateHashes(GameModeMaps.GameModes);

        gameFilesHash = fhc.GetCompleteHash();

        if (isHost)
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} +klnNs {1} {2}", channel.ChannelName,
                channel.Password, playerLimit),
                QueuedMessageType.SYSTEMMESSAGE, 50));

            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("TOPIC {0} :{1}", channel.ChannelName,
                ProgramConstants.CNCNETPROTOCOLREVISION + ";" + localGame.ToLower()),
                QueuedMessageType.SYSTEMMESSAGE, 50));

            gameBroadcastTimer.Enabled = true;
            gameBroadcastTimer.Start();
            gameBroadcastTimer.SetTime(TimeSpan.FromSeconds(INITIAL_GAME_BROADCAST_DELAY));
        }
        else
        {
            channel.SendCTCPMessage("FHSH " + gameFilesHash, QueuedMessageType.SYSTEMMESSAGE, 10);
        }

        topBar.AddPrimarySwitchable(this);
        topBar.SwitchToPrimary();
        WindowManager.SelectedControl = tbChatInput;
        ResetAutoReadyCheckbox();
        UpdatePing();
        UpdateDiscordPresence(true);
    }

    public void ChangeChatColor(IRCColor chatColor)
    {
        this.chatColor = chatColor;
        tbChatInput.TextColor = chatColor.XnaColor;
    }

    private void MultiplayerName_RightClick(object sender, MultiplayerNameRightClickedEventArgs args)
    {
        globalContextMenu.Show(
            new GlobalContextMenuData()
        {
            PlayerName = args.PlayerName,
            PreventJoinGame = true
        }, GetCursorPoint());
    }

    private void BtnChangeTunnel_LeftClick(object sender, EventArgs e) => ShowTunnelSelectionWindow("Select tunnel server:".L10N("UI:Main:SelectTunnelServer"));

    private void GameBroadcastTimer_TimeElapsed(object sender, EventArgs e) => BroadcastGame();

    private void TunnelHandler_CurrentTunnelPinged(object sender, EventArgs e) => UpdatePing();

    private void UpdatePing()
    {
        if (tunnelHandler.CurrentTunnel == null)
            return;

        channel.SendCTCPMessage("TNLPNG " + tunnelHandler.CurrentTunnel.PingInMs, QueuedMessageType.SYSTEMMESSAGE, 10);

        PlayerInfo pInfo = players.Find(p => p.Name.Equals(ProgramConstants.PLAYERNAME, StringComparison.Ordinal));
        if (pInfo != null)
        {
            pInfo.Ping = tunnelHandler.CurrentTunnel.PingInMs;
            UpdatePlayerPingIndicator(pInfo);
        }
    }

    private void PrintTunnelServerInformation(string s)
    {
        if (tunnelHandler.CurrentTunnel == null)
        {
            AddNotice("Tunnel server unavailable!".L10N("UI:Main:TunnelUnavailable"));
        }
        else
        {
            AddNotice(string.Format(
                "Current tunnel server: {0} {1} (Players: {2}/{3}) (Official: {4})".L10N("UI:Main:TunnelInfo"),
                    tunnelHandler.CurrentTunnel.Name, tunnelHandler.CurrentTunnel.Country, tunnelHandler.CurrentTunnel.Clients, tunnelHandler.CurrentTunnel.MaxClients, tunnelHandler.CurrentTunnel.Official));
        }
    }

    private void ShowTunnelSelectionWindow(string description)
    {
        tunnelSelectionWindow.Open(
            description,
            tunnelHandler.CurrentTunnel?.Address);
    }

    private void TunnelSelectionWindow_TunnelSelected(object sender, TunnelEventArgs e)
    {
        channel.SendCTCPMessage(
            $"{CHANGE_TUNNEL_SERVER_MESSAGE} {e.Tunnel.Address}:{e.Tunnel.Port}",
            QueuedMessageType.SYSTEMMESSAGE, 10);
        HandleTunnelServerChange(e.Tunnel);
    }

    public override void Clear()
    {
        base.Clear();

        if (channel != null)
        {
            channel.MessageAdded -= Channel_MessageAdded;
            channel.CTCPReceived -= Channel_CTCPReceived;
            channel.UserKicked -= Channel_UserKicked;
            channel.UserQuitIRC -= Channel_UserQuitIRC;
            channel.UserLeft -= Channel_UserLeft;
            channel.UserAdded -= Channel_UserAdded;
            channel.UserNameChanged -= Channel_UserNameChanged;
            channel.UserListReceived -= Channel_UserListReceived;

            if (!isHost)
            {
                channel.ChannelModesChanged -= Channel_ChannelModesChanged;
            }

            connectionManager.RemoveChannel(channel);
        }

        Disable();
        connectionManager.ConnectionLost -= ConnectionManager_ConnectionLost;
        connectionManager.Disconnected -= ConnectionManager_Disconnected;

        gameBroadcastTimer.Enabled = false;
        closed = false;

        tbChatInput.Text = string.Empty;

        tunnelHandler.CurrentTunnel = null;
        tunnelHandler.CurrentTunnelPinged -= TunnelHandler_CurrentTunnelPinged;

        GameLeft?.Invoke(this, EventArgs.Empty);

        topBar.RemovePrimarySwitchable(this);
        ResetDiscordPresence();
    }

    public void LeaveGameLobby()
    {
        if (isHost)
        {
            closed = true;
            BroadcastGame();
        }

        Clear();
        channel.Leave();
    }

    public override string GetSwitchName() => "Game Lobby".L10N("UI:Main:GameLobby");

    protected override void BtnLeaveGameLeftClick(object sender, EventArgs e) => LeaveGameLobby();

    private void ConnectionManager_Disconnected(object sender, EventArgs e) => HandleConnectionLoss();

    private void ConnectionManager_ConnectionLost(object sender, ConnectionLostEventArgs e) => HandleConnectionLoss();

    private void HandleConnectionLoss()
    {
        Clear();
        Disable();
    }

    private void Channel_UserNameChanged(object sender, UserNameChangedEventArgs e)
    {
        Logger.Log("CnCNetGameLobby: Nickname change: " + e.OldUserName + " to " + e.User.Name);
        int index = players.FindIndex(p => p.Name == e.OldUserName);
        if (index > -1)
        {
            PlayerInfo player = players[index];
            player.Name = e.User.Name;
            ddPlayerNames[index].Items[0].Text = player.Name;
            AddNotice(string.Format("Player {0} changed their name to {1}".L10N("UI:Main:PlayerRename"), e.OldUserName, e.User.Name));
        }
    }

    protected override void UpdateDiscordPresence(bool resetTimer = false)
    {
        if (discordHandler == null)
            return;

        PlayerInfo player = FindLocalPlayer();
        if (player == null || Map == null || GameMode == null)
            return;
        string side = string.Empty;
        if (ddPlayerSides.Length > players.IndexOf(player))
            side = ddPlayerSides[players.IndexOf(player)].SelectedItem.Text;
        string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby"; // not UI strings

        discordHandler.UpdatePresence(
            Map.Name, GameMode.Name, "Multiplayer",
            currentState, players.Count, playerLimit, side,
            channel.UIName, isHost, isCustomPassword, Locked, resetTimer);
    }

    /// <summary>
    /// Starts the game for the game host.
    /// </summary>
    protected override void HostLaunchGame()
    {
        if (players.Count > 1)
        {
            AddNotice("Contacting tunnel server...".L10N("UI:Main:ConnectingTunnel"));

            List<int> playerPorts = tunnelHandler.CurrentTunnel.GetPlayerPortInfo(players.Count);

            if (playerPorts.Count < players.Count)
            {
                ShowTunnelSelectionWindow(("An error occured while contacting " +
                    "the CnCNet tunnel server." + Environment.NewLine +
                    "Try picking a different tunnel server:").L10N("UI:Main:ConnectTunnelError1"));
                AddNotice(
                    ("An error occured while contacting the specified CnCNet " +
                    "tunnel server. Please try using a different tunnel server ").L10N("UI:Main:ConnectTunnelError2"), ERROR_MESSAGE_COLOR);
                return;
            }

            StringBuilder sb = new("START ");
            _ = sb.Append(UniqueGameID);
            for (int pId = 0; pId < players.Count; pId++)
            {
                players[pId].Port = playerPorts[pId];
                _ = sb.Append(';');
                _ = sb.Append(players[pId].Name);
                _ = sb.Append(';');
                _ = sb.Append("0.0.0.0:");
                _ = sb.Append(playerPorts[pId]);
            }

            channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEMMESSAGE, 10);
        }
        else
        {
            Logger.Log("One player MP -- starting!");
        }

        players.ForEach(pInfo => pInfo.IsInGame = true);

        cncnetUserData.AddRecentPlayers(players.Select(p => p.Name), channel.UIName);

        StartGame();
    }

    private void Channel_UserQuitIRC(object sender, UserNameEventArgs e)
    {
        RemovePlayer(e.UserName);

        if (e.UserName == hostName)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(
                ERROR_MESSAGE_COLOR, "The game host abandoned the game.".L10N("UI:Main:HostAbandoned")));
            BtnLeaveGameLeftClick(this, EventArgs.Empty);
        }
        else
        {
            UpdateDiscordPresence();
        }
    }

    private void Channel_UserLeft(object sender, UserNameEventArgs e)
    {
        RemovePlayer(e.UserName);

        if (e.UserName == hostName)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(
                ERROR_MESSAGE_COLOR, "The game host abandoned the game.".L10N("UI:Main:HostAbandoned")));
            BtnLeaveGameLeftClick(this, EventArgs.Empty);
        }
        else
        {
            UpdateDiscordPresence();
        }
    }

    private void Channel_UserKicked(object sender, UserNameEventArgs e)
    {
        if (e.UserName == ProgramConstants.PLAYERNAME)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(
                ERROR_MESSAGE_COLOR, "You were kicked from the game!".L10N("UI:Main:YouWereKicked")));
            Clear();
            Visible = false;
            Enabled = false;
            return;
        }

        int index = players.FindIndex(p => p.Name == e.UserName);

        if (index > -1)
        {
            players.RemoveAt(index);
            CopyPlayerDataToUI();
            UpdateDiscordPresence();
            ClearReadyStatuses();
        }
    }

    private void Channel_UserListReceived(object sender, EventArgs e)
    {
        if (!isHost)
        {
            if (channel.Users.Find(hostName) == null)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "The game host has abandoned the game.".L10N("UI:Main:HostHasAbandoned")));
                BtnLeaveGameLeftClick(this, EventArgs.Empty);
            }
        }

        UpdateDiscordPresence();
    }

    private void Channel_UserAdded(object sender, ChannelUserEventArgs e)
    {
        PlayerInfo pInfo = new(e.User.IRCUser.Name);
        players.Add(pInfo);

        if (players.Count + aIPlayers.Count > MAXPLAYERCOUNT && aIPlayers.Count > 0)
            aIPlayers.RemoveAt(aIPlayers.Count - 1);

        sndJoinSound.Play();

        WindowManager.FlashWindow();

        if (!isHost)
        {
            CopyPlayerDataToUI();
            return;
        }

        if (e.User.IRCUser.Name != ProgramConstants.PLAYERNAME)
        {
            // Changing the map applies forced settings (co-op sides etc.) to the
            // new player, and it also sends an options broadcast message
            //CopyPlayerDataToUI(); This is also called by ChangeMap()
            ChangeMap(GameModeMap);
            BroadcastPlayerOptions();
            BroadcastPlayerExtraOptions();
            UpdateDiscordPresence();
        }
        else
        {
            players[0].Ready = true;
            CopyPlayerDataToUI();
        }

        if (players.Count >= playerLimit)
        {
            AddNotice("Player limit reached. The game room has been locked.".L10N("UI:Main:GameRoomNumberLimitReached"));
            LockGame();
        }
    }

    private void RemovePlayer(string playerName)
    {
        PlayerInfo pInfo = players.Find(p => p.Name == playerName);

        if (pInfo != null)
        {
            _ = players.Remove(pInfo);

            CopyPlayerDataToUI();

            // This might not be necessary
            if (isHost)
                BroadcastPlayerOptions();
        }

        sndLeaveSound.Play();

        if (isHost && Locked && !ProgramConstants.IsInGame)
        {
            UnlockGame(true);
        }
    }

    private void Channel_ChannelModesChanged(object sender, ChannelModeEventArgs e)
    {
        if (e.ModeString == "+i")
        {
            if (players.Count >= playerLimit)
                AddNotice("Player limit reached. The game room has been locked.".L10N("UI:Main:GameRoomNumberLimitReached"));
            else
                AddNotice("The game host has locked the game room.".L10N("UI:Main:RoomLockedByHost"));
        }
        else if (e.ModeString == "-i")
        {
            AddNotice("The game room has been unlocked.".L10N("UI:Main:GameRoomUnlocked"));
        }
    }

    private void Channel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
    {
        Logger.Log("CnCNetGameLobby_CTCPReceived");

        foreach (CommandHandlerBase cmdHandler in ctcpCommandHandlers)
        {
            if (cmdHandler.Handle(e.UserName, e.Message))
            {
                UpdateDiscordPresence();
                return;
            }
        }

        Logger.Log("Unhandled CTCP command: " + e.Message + " from " + e.UserName);
    }

    private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
    {
        if (cncnetUserData.IsIgnored(e.Message.SenderIdent))
        {
            lbChatMessages.AddMessage(new ChatMessage(
                Color.Silver,
                string.Format("Message blocked from {0}".L10N("UI:Main:MessageBlockedFromPlayer"), e.Message.SenderName)));
        }
        else
        {
            lbChatMessages.AddMessage(e.Message);

            if (e.Message.SenderName != null)
                sndMessageSound.Play();
        }
    }

    protected override void RequestPlayerOptions(int side, int color, int start, int team)
    {
        byte[] value = new byte[]
        {
            (byte)side,
            (byte)color,
            (byte)start,
            (byte)team
        };

        int intValue = BitConverter.ToInt32(value, 0);

        channel.SendCTCPMessage(
            string.Format("OR {0}", intValue),
            QueuedMessageType.GAMESETTINGSMESSAGE, 6);
    }

    protected override void RequestReadyStatus()
    {
        if (Map == null || GameMode == null)
        {
            AddNotice(("The game host needs to select a different map or " +
                "you will be unable to participate in the match.").L10N("UI:Main:HostMustReplaceMap"));

            if (chkAutoReady.Checked)
                channel.SendCTCPMessage("R 0", QueuedMessageType.GAMEPLAYERSREADYSTATUSMESSAGE, 5);

            return;
        }

        PlayerInfo pInfo = players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
        int readyState = 0;

        if (chkAutoReady.Checked)
            readyState = 2;
        else if (!pInfo.Ready)
            readyState = 1;

        channel.SendCTCPMessage($"R {readyState}", QueuedMessageType.GAMEPLAYERSREADYSTATUSMESSAGE, 5);
    }

    protected override void AddNotice(string message, Color color) => channel.AddMessage(new ChatMessage(color, message));

    /// <summary>
    /// Broadcasts player options to non-host players.
    /// </summary>
    protected override void BroadcastPlayerOptions()
    {
        // Broadcast player options
        StringBuilder sb = new("PO ");
        foreach (PlayerInfo pInfo in players.Concat(aIPlayers))
        {
            _ = pInfo.IsAI ? sb.Append(pInfo.AILevel) : sb.Append(pInfo.Name);
            _ = sb.Append(';');

            // Combine the options into one integer to save bandwidth in
            // cases where the player uses default options (this is common for AI players)
            // Will hopefully make GameSurge kicking people a bit less common
            byte[] byteArray = new byte[]
            {
                (byte)pInfo.TeamId,
                (byte)pInfo.StartingLocation,
                (byte)pInfo.ColorId,
                (byte)pInfo.SideId,
            };

            int value = BitConverter.ToInt32(byteArray, 0);
            _ = sb.Append(value);
            _ = sb.Append(';');
            if (!pInfo.IsAI)
            {
                _ = pInfo.AutoReady && !pInfo.IsInGame ? sb.Append(2) : sb.Append(Convert.ToInt32(pInfo.Ready));
                _ = sb.Append(';');
            }
        }

        channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAMEPLAYERSMESSAGE, 11);
    }

    /// <summary>
    /// Handles player option requests received from non-host players.
    /// </summary>
    private void HandleOptionsRequest(string playerName, int options)
    {
        if (!isHost)
            return;

        if (ProgramConstants.IsInGame)
            return;

        PlayerInfo pInfo = players.Find(p => p.Name == playerName);

        if (pInfo == null)
            return;

        byte[] bytes = BitConverter.GetBytes(options);

        int side = bytes[0];
        int color = bytes[1];
        int start = bytes[2];
        int team = bytes[3];

        if (side < 0 || side > SideCount + RandomSelectorCount)
            return;

        if (color < 0 || color > mPColors.Count)
            return;

        bool[] disallowedSides = GetDisallowedSides();

        if (side > 0 && side <= SideCount && disallowedSides[side - 1])
            return;

        if (Map.CoopInfo != null)
        {
            if (Map.CoopInfo.DisallowedPlayerSides.Contains(side - 1) || side == SideCount + RandomSelectorCount)
                return;

            if (Map.CoopInfo.DisallowedPlayerColors.Contains(color - 1))
                return;
        }

        if (start < 0 || start > Map.MaxPlayers)
            return;

        if (team is < 0 or > 4)
            return;

        if (side != pInfo.SideId
            || start != pInfo.StartingLocation
            || team != pInfo.TeamId)
        {
            ClearReadyStatuses();
        }

        pInfo.SideId = side;
        pInfo.ColorId = color;
        pInfo.StartingLocation = start;
        pInfo.TeamId = team;

        CopyPlayerDataToUI();
        BroadcastPlayerOptions();
    }

    /// <summary>
    /// Handles "I'm ready" messages received from non-host players.
    /// </summary>
    private void HandleReadyRequest(string playerName, int readyStatus)
    {
        if (!isHost)
            return;

        PlayerInfo pInfo = players.Find(p => p.Name == playerName);

        if (pInfo == null)
            return;

        pInfo.Ready = readyStatus > 0;
        pInfo.AutoReady = readyStatus > 1;

        CopyPlayerDataToUI();
        BroadcastPlayerOptions();
    }

    protected override void PlayerExtraOptionsOptionsChanged(object sender, EventArgs e)
    {
        base.PlayerExtraOptionsOptionsChanged(sender, e);
        BroadcastPlayerExtraOptions();
    }

    protected override void BroadcastPlayerExtraOptions()
    {
        if (!isHost)
            return;

        PlayerExtraOptions playerExtraOptions = GetPlayerExtraOptions();

        channel.SendCTCPMessage(playerExtraOptions.ToCncnetMessage(), QueuedMessageType.GAMEPLAYERSEXTRAMESSAGE, 11, true);
    }

    /// <summary>
    /// Broadcasts game options to non-host players
    /// when the host has changed an option.
    /// </summary>
    protected override void OnGameOptionChanged()
    {
        base.OnGameOptionChanged();

        if (!isHost)
            return;

        bool[] optionValues = new bool[CheckBoxes.Count];
        for (int i = 0; i < CheckBoxes.Count; i++)
            optionValues[i] = CheckBoxes[i].Checked;

        // Let's pack the booleans into bytes
        List<byte> byteList = Conversions.BoolArrayIntoBytes(optionValues).ToList();

        while (byteList.Count % 4 != 0)
            byteList.Add(0);

        int integerCount = byteList.Count / 4;
        byte[] byteArray = byteList.ToArray();

        ExtendedStringBuilder sb = new("GO ", true, ';');

        for (int i = 0; i < integerCount; i++)
            sb.Append(BitConverter.ToInt32(byteArray, i * 4));

        // We don't gain much in most cases by packing the drop-down values
        // (because they're bytes to begin with, and usually non-zero),
        // so let's just transfer them as usual
        foreach (GameLobbyDropDown dd in DropDowns)
            sb.Append(dd.SelectedIndex);

        sb.Append(Convert.ToInt32(Map.Official));
        sb.Append(Map.SHA1);
        sb.Append(GameMode.Name);
        sb.Append(FrameSendRate);
        sb.Append(MaxAhead);
        sb.Append(ProtocolVersion);
        sb.Append(RandomSeed);
        sb.Append(Convert.ToInt32(RemoveStartingLocations));
        sb.Append(Map.Name);

        channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAMESETTINGSMESSAGE, 11);
    }

    protected override void ChangeMap(GameModeMap gameModeMap)
    {
        mapSharingConfirmationPanel.Disable();
        base.ChangeMap(gameModeMap);
    }

    /// <summary>
    /// Handles player option messages received from the game host.
    /// </summary>
    private void ApplyPlayerOptions(string sender, string message)
    {
        if (sender != hostName)
            return;

        players.Clear();
        aIPlayers.Clear();

        string[] parts = message.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length;)
        {
            PlayerInfo pInfo = new();

            string pName = parts[i];
            int converted = Conversions.IntFromString(pName, -1);

            if (converted > -1)
            {
                pInfo.IsAI = true;
                pInfo.AILevel = converted;
                pInfo.Name = GameLobbyBase.AILevelToName(converted);
            }
            else
            {
                pInfo.Name = pName;

                // If we can't find the player from the channel user list,
                // ignore the player
                // They've either left the channel or got kicked before the
                // player options message reached us
                if (channel.Users.Find(pName) == null)
                {
                    i += HUMAN_PLAYER_OPTIONS_LENGTH;
                    continue;
                }
            }

            if (parts.Length <= i + 1)
                return;

            int playerOptions = Conversions.IntFromString(parts[i + 1], -1);
            if (playerOptions == -1)
                return;

            byte[] byteArray = BitConverter.GetBytes(playerOptions);

            int team = byteArray[0];
            int start = byteArray[1];
            int color = byteArray[2];
            int side = byteArray[3];

            if (side < 0 || side > SideCount + RandomSelectorCount)
                return;

            if (color < 0 || color > mPColors.Count)
                return;

            if (start is < 0 or > MAXPLAYERCOUNT)
                return;

            if (team is < 0 or > 4)
                return;

            pInfo.TeamId = byteArray[0];
            pInfo.StartingLocation = byteArray[1];
            pInfo.ColorId = byteArray[2];
            pInfo.SideId = byteArray[3];

            if (pInfo.IsAI)
            {
                pInfo.Ready = true;
                aIPlayers.Add(pInfo);
                i += AI_PLAYER_OPTIONS_LENGTH;
            }
            else
            {
                if (parts.Length <= i + 2)
                    return;

                int readyStatus = Conversions.IntFromString(parts[i + 2], -1);

                if (readyStatus == -1)
                    return;

                pInfo.Ready = readyStatus > 0;
                pInfo.AutoReady = readyStatus > 1;
                if (pInfo.Name == ProgramConstants.PLAYERNAME)
                    btnLaunchGame.Text = pInfo.Ready ? BTN_LAUNCH_NOT_READY : BTN_LAUNCH_READY;

                players.Add(pInfo);
                i += HUMAN_PLAYER_OPTIONS_LENGTH;
            }
        }

        CopyPlayerDataToUI();
    }

    /// <summary>
    /// Handles game option messages received from the game host.
    /// </summary>
    private void ApplyGameOptions(string sender, string message)
    {
        if (sender != hostName)
            return;

        string[] parts = message.Split(';');

        int checkBoxIntegerCount = (CheckBoxes.Count / 32) + 1;

        int partIndex = checkBoxIntegerCount + DropDowns.Count;

        if (parts.Length < partIndex + 6)
        {
            AddNotice(
                ("The game host has sent an invalid game options message! " +
                "The game host's game version might be different from yours.").L10N("UI:Main:HostGameOptionInvalid"), Color.Red);
            return;
        }

        string mapOfficial = parts[partIndex];
        bool isMapOfficial = Conversions.BooleanFromString(mapOfficial, true);

        string mapSHA1 = parts[partIndex + 1];

        string gameMode = parts[partIndex + 2];

        int frameSendRate = Conversions.IntFromString(parts[partIndex + 3], FrameSendRate);
        if (frameSendRate != FrameSendRate)
        {
            FrameSendRate = frameSendRate;
            AddNotice(string.Format("The game host has changed FrameSendRate (order lag) to {0}".L10N("UI:Main:HostChangeFrameSendRate"), frameSendRate));
        }

        int maxAhead = Conversions.IntFromString(parts[partIndex + 4], MaxAhead);
        if (maxAhead != MaxAhead)
        {
            MaxAhead = maxAhead;
            AddNotice(string.Format("The game host has changed MaxAhead to {0}".L10N("UI:Main:HostChangeMaxAhead"), maxAhead));
        }

        int protocolVersion = Conversions.IntFromString(parts[partIndex + 5], ProtocolVersion);
        if (protocolVersion != ProtocolVersion)
        {
            ProtocolVersion = protocolVersion;
            AddNotice(string.Format("The game host has changed ProtocolVersion to {0}".L10N("UI:Main:HostChangeProtocolVersion"), protocolVersion));
        }

        string mapName = parts[partIndex + 8];
        GameModeMap currentGameModeMap = GameModeMap;

        lastGameMode = gameMode;
        lastMapSHA1 = mapSHA1;
        lastMapName = mapName;

        GameModeMap = GameModeMaps.Find(gmm => gmm.GameMode.UIName == gameMode && gmm.Map.SHA1 == mapSHA1);
        if (GameModeMap == null)
        {
            ChangeMap(null);

            if (!isMapOfficial)
                RequestMap(mapSHA1);
            else
                ShowOfficialMapMissingMessage(mapSHA1);
        }
        else if (GameModeMap != currentGameModeMap)
        {
            ChangeMap(GameModeMap);
        }

        // By changing the game options after changing the map, we know which
        // game options were changed by the map and which were changed by the game host

        // If the map doesn't exist on the local installation, it's impossible
        // to know which options were set by the host and which were set by the
        // map, so we'll just assume that the host has set all the options.
        // Very few (if any) custom maps force options, so it'll be correct nearly always
        for (int i = 0; i < checkBoxIntegerCount; i++)
        {
            if (parts.Length <= i)
                return;

            bool success = int.TryParse(parts[i], out int checkBoxStatusInt);

            if (!success)
            {
                AddNotice(
                    ("Failed to parse check box options sent by game host!" +
                    "The game host's game version might be different from yours.").L10N("UI:Main:HostCheckBoxParseError"), Color.Red);
                return;
            }

            byte[] byteArray = BitConverter.GetBytes(checkBoxStatusInt);
            bool[] boolArray = Conversions.BytesIntoBoolArray(byteArray);

            for (int optionIndex = 0; optionIndex < boolArray.Length; optionIndex++)
            {
                int gameOptionIndex = (i * 32) + optionIndex;

                if (gameOptionIndex >= CheckBoxes.Count)
                    break;

                GameLobbyCheckBox checkBox = CheckBoxes[gameOptionIndex];

                if (checkBox.Checked != boolArray[optionIndex])
                {
                    if (boolArray[optionIndex])
                        AddNotice(string.Format("The game host has enabled {0}".L10N("UI:Main:HostEnableOption"), checkBox.Text));
                    else
                        AddNotice(string.Format("The game host has disabled {0}".L10N("UI:Main:HostDisableOption"), checkBox.Text));
                }

                CheckBoxes[gameOptionIndex].Checked = boolArray[optionIndex];
            }
        }

        for (int i = checkBoxIntegerCount; i < DropDowns.Count + checkBoxIntegerCount; i++)
        {
            if (parts.Length <= i)
            {
                AddNotice(
                    ("The game host has sent an invalid game options message! " +
                "The game host's game version might be different from yours.").L10N("UI:Main:HostGameOptionInvalid"), Color.Red);
                return;
            }

            bool success = int.TryParse(parts[i], out int ddSelectedIndex);

            if (!success)
            {
                AddNotice(
                    ("Failed to parse drop down options sent by game host (2)! " +
                    "The game host's game version might be different from yours.").L10N("UI:Main:HostGameOptionInvalidTheSecondTime"), Color.Red);
                return;
            }

            GameLobbyDropDown dd = DropDowns[i - checkBoxIntegerCount];

            if (ddSelectedIndex < -1 || ddSelectedIndex >= dd.Items.Count)
                continue;

            if (dd.SelectedIndex != ddSelectedIndex)
            {
                string ddName = dd.OptionName;
                if (dd.OptionName == null)
                    ddName = dd.Name;

                AddNotice(string.Format("The game host has set {0} to {1}".L10N("UI:Main:HostSetOption"), ddName, dd.Items[ddSelectedIndex].Text));
            }

            DropDowns[i - checkBoxIntegerCount].SelectedIndex = ddSelectedIndex;
        }

        bool parseSuccess = int.TryParse(parts[partIndex + 6], out int randomSeed);

        if (!parseSuccess)
        {
            AddNotice(
                ("Failed to parse random seed from game options message! " +
                "The game host's game version might be different from yours.").L10N("UI:Main:HostRandomSeedError"), Color.Red);
        }

        bool removeStartingLocations = Convert.ToBoolean(Conversions.IntFromString(
            parts[partIndex + 7],
            Convert.ToInt32(RemoveStartingLocations)));
        SetRandomStartingLocations(removeStartingLocations);

        RandomSeed = randomSeed;
    }

    private void RequestMap(string mapSHA1)
    {
        if (UserINISettings.Instance.EnableMapSharing)
        {
            AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("UI:Main:MapNotExist"));
            mapSharingConfirmationPanel.ShowForMapDownload();
        }
        else
        {
            AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("UI:Main:MapNotExist") + " " +
                ("Because you've disabled map sharing, it cannot be transferred. The game host needs " +
                "to change the map or you will be unable to participate in the match.").L10N("UI:Main:MapSharingDisabledNotice"));
            channel.SendCTCPMessage(MAP_SHARING_DISABLED_MESSAGE, QueuedMessageType.SYSTEMMESSAGE, 9);
        }
    }

    private void ShowOfficialMapMissingMessage(string sha1)
    {
        AddNotice(("The game host has selected an official map that doesn't exist on your installation. " +
            "This could mean that the game host has modified game files, or is running a different game version. " +
            "They need to change the map or you will be unable to participate in the match.").L10N("UI:Main:OfficialMapNotExist"));
        channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + sha1, QueuedMessageType.SYSTEMMESSAGE, 9);
    }

    private void MapSharingConfirmationPanel_MapDownloadConfirmed(object sender, EventArgs e)
    {
        Logger.Log("Map sharing confirmed.");
        AddNotice("Attempting to download map.".L10N("UI:Main:DownloadingMap"));
        mapSharingConfirmationPanel.SetDownloadingStatus();
        MapSharer.DownloadMap(lastMapSHA1, localGame, lastMapName);
    }

    /// <summary>
    /// Signals other players that the local player has returned from the game,
    /// and unlocks the game as well as generates a new random seed as the game host.
    /// </summary>
    protected override void GameProcessExited()
    {
        base.GameProcessExited();

        channel.SendCTCPMessage("RETURN", QueuedMessageType.SYSTEMMESSAGE, 20);
        ReturnNotification(ProgramConstants.PLAYERNAME);

        if (isHost)
        {
            RandomSeed = new Random().Next();
            OnGameOptionChanged();
            ClearReadyStatuses();
            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
            BroadcastPlayerExtraOptions();

            if (players.Count < playerLimit)
                UnlockGame(true);
        }
    }

    protected override void StartGame()
    {
        AddNotice("Starting game...".L10N("UI:Main:StartingGame"));

        FileHashCalculator fhc = new();
        fhc.CalculateHashes(GameModeMaps.GameModes);

        if (gameFilesHash != fhc.GetCompleteHash())
        {
            Logger.Log("Game files modified during client session!");
            channel.SendCTCPMessage(CHEAT_DETECTED_MESSAGE, QueuedMessageType.INSTANTMESSAGE, 0);
            HandleCheatDetectedMessage(ProgramConstants.PLAYERNAME);
        }

        base.StartGame();
    }

    /// <summary>
    /// Handles the "START" (game start) command sent by the game host.
    /// </summary>
    private void NonHostLaunchGame(string sender, string message)
    {
        if (sender != hostName)
            return;

        string[] parts = message.Split(';');

        if (parts.Length < 1)
            return;

        UniqueGameID = Conversions.IntFromString(parts[0], -1);
        if (UniqueGameID < 0)
            return;

        List<string> recentPlayers = new();

        for (int i = 1; i < parts.Length; i += 2)
        {
            if (parts.Length <= i + 1)
                return;

            string pName = parts[i];
            string[] ipAndPort = parts[i + 1].Split(':');

            if (ipAndPort.Length < 2)
                return;

            bool success = int.TryParse(ipAndPort[1], out int port);

            if (!success)
                return;

            PlayerInfo pInfo = players.Find(p => p.Name == pName);

            if (pInfo == null)
                return;

            pInfo.Port = port;
            recentPlayers.Add(pName);
        }

        cncnetUserData.AddRecentPlayers(recentPlayers, channel.UIName);

        StartGame();
    }

    protected override void WriteSpawnIniAdditions(IniFile iniFile)
    {
        base.WriteSpawnIniAdditions(iniFile);

        iniFile.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
        iniFile.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);

        iniFile.SetIntValue("Settings", "GameID", UniqueGameID);
        iniFile.SetBooleanValue("Settings", "Host", isHost);

        PlayerInfo localPlayer = FindLocalPlayer();

        if (localPlayer == null)
            return;

        iniFile.SetIntValue("Settings", "Port", localPlayer.Port);
    }

    protected override void SendChatMessage(string message) => channel.SendChatMessage(message, chatColor);

    protected override void GetReadyNotification()
    {
        base.GetReadyNotification();

        WindowManager.FlashWindow();
        topBar.SwitchToPrimary();

        if (isHost)
            channel.SendCTCPMessage("GETREADY", QueuedMessageType.GAMEGETREADYMESSAGE, 0);
    }

    private void HandleNotification(string sender, Action handler)
    {
        if (sender != hostName)
            return;

        handler();
    }

    private void HandleIntNotification(string sender, int parameter, Action<int> handler)
    {
        if (sender != hostName)
            return;

        handler(parameter);
    }

    protected override void AISpectatorsNotification()
    {
        base.AISpectatorsNotification();

        if (isHost)
            channel.SendCTCPMessage("AISPECS", QueuedMessageType.GAMENOTIFICATIONMESSAGE, 0);
    }

    protected override void InsufficientPlayersNotification()
    {
        base.InsufficientPlayersNotification();

        if (isHost)
            channel.SendCTCPMessage("INSFSPLRS", QueuedMessageType.GAMENOTIFICATIONMESSAGE, 0);
    }

    protected override void TooManyPlayersNotification()
    {
        base.TooManyPlayersNotification();

        if (isHost)
            channel.SendCTCPMessage("TMPLRS", QueuedMessageType.GAMENOTIFICATIONMESSAGE, 0);
    }

    protected override void SharedColorsNotification()
    {
        base.SharedColorsNotification();

        if (isHost)
            channel.SendCTCPMessage("CLRS", QueuedMessageType.GAMENOTIFICATIONMESSAGE, 0);
    }

    protected override void SharedStartingLocationNotification()
    {
        base.SharedStartingLocationNotification();

        if (isHost)
            channel.SendCTCPMessage("SLOC", QueuedMessageType.GAMENOTIFICATIONMESSAGE, 0);
    }

    protected override void LockGameNotification()
    {
        base.LockGameNotification();

        if (isHost)
            channel.SendCTCPMessage("LCKGME", QueuedMessageType.GAMENOTIFICATIONMESSAGE, 0);
    }

    protected override void NotVerifiedNotification(int playerIndex)
    {
        base.NotVerifiedNotification(playerIndex);

        if (isHost)
            channel.SendCTCPMessage("NVRFY " + playerIndex, QueuedMessageType.GAMENOTIFICATIONMESSAGE, 0);
    }

    protected override void StillInGameNotification(int playerIndex)
    {
        base.StillInGameNotification(playerIndex);

        if (isHost)
            channel.SendCTCPMessage("INGM " + playerIndex, QueuedMessageType.GAMENOTIFICATIONMESSAGE, 0);
    }

    protected override void BroadcastDiceRoll(int dieSides, int[] results)
    {
        string resultString = string.Join(",", results);
        channel.SendCTCPMessage($"{DICE_ROLL_MESSAGE} {dieSides},{resultString}", QueuedMessageType.CHATMESSAGE, 0);
        PrintDiceRollResult(ProgramConstants.PLAYERNAME, dieSides, results);
    }

    private void ReturnNotification(string sender)
    {
        AddNotice(string.Format("{0} has returned from the game.".L10N("UI:Main:PlayerReturned"), sender));

        PlayerInfo pInfo = players.Find(p => p.Name == sender);

        if (pInfo != null)
            pInfo.IsInGame = false;

        sndReturnSound.Play();
    }

    private void HandleTunnelPing(string sender, int ping)
    {
        PlayerInfo pInfo = players.Find(p => p.Name.Equals(sender, StringComparison.Ordinal));
        if (pInfo != null)
        {
            pInfo.Ping = ping;
            UpdatePlayerPingIndicator(pInfo);
        }
    }

    private void FileHashNotification(string sender, string filesHash)
    {
        if (!isHost)
            return;

        PlayerInfo pInfo = players.Find(p => p.Name == sender);

        if (pInfo != null)
            pInfo.Verified = true;

        if (filesHash != gameFilesHash)
        {
            channel.SendCTCPMessage("MM " + sender, QueuedMessageType.GAMECHEATERMESSAGE, 10);
            CheaterNotification(ProgramConstants.PLAYERNAME, sender);
        }
    }

    private void CheaterNotification(string sender, string cheaterName)
    {
        if (sender != hostName)
            return;

        AddNotice(string.Format("Player {0} has different files compared to the game host. Either {0} or the game host could be cheating.".L10N("UI:Main:DifferentFileCheating"), cheaterName), Color.Red);
    }

    protected override void HandleLockGameButtonClick()
    {
        if (!Locked)
        {
            AddNotice("You've locked the game room.".L10N("UI:Main:RoomLockedByYou"));
            LockGame();
        }
        else
        {
            if (players.Count < playerLimit)
            {
                AddNotice("You've unlocked the game room.".L10N("UI:Main:RoomUnockedByYou"));
                UnlockGame(false);
            }
            else
            {
                AddNotice(string.Format(
                    "Cannot unlock game; the player limit ({0}) has been reached.".L10N("UI:Main:RoomCantUnlockAsLimit"), playerLimit));
            }
        }
    }

    protected override void LockGame()
    {
        connectionManager.SendCustomMessage(new QueuedMessage(
            string.Format("MODE {0} +i", channel.ChannelName), QueuedMessageType.INSTANTMESSAGE, -1));

        Locked = true;
        btnLockGame.Text = "Unlock Game".L10N("UI:Main:UnlockGame");
        AccelerateGameBroadcasting();
    }

    protected override void UnlockGame(bool manual)
    {
        connectionManager.SendCustomMessage(new QueuedMessage(
            string.Format("MODE {0} -i", channel.ChannelName), QueuedMessageType.INSTANTMESSAGE, -1));

        Locked = false;
        if (manual)
            AddNotice("The game room has been unlocked.".L10N("UI:Main:GameRoomUnlocked"));
        btnLockGame.Text = "Lock Game".L10N("UI:Main:LockGame");
        AccelerateGameBroadcasting();
    }

    protected override void KickPlayer(int playerIndex)
    {
        if (playerIndex >= players.Count)
            return;

        PlayerInfo pInfo = players[playerIndex];

        AddNotice(string.Format("Kicking {0} from the game...".L10N("UI:Main:KickPlayer"), pInfo.Name));
        channel.SendKickMessage(pInfo.Name, 8);
    }

    protected override void BanPlayer(int playerIndex)
    {
        if (playerIndex >= players.Count)
            return;

        PlayerInfo pInfo = players[playerIndex];

        IRCUser user = connectionManager.UserList.Find(u => u.Name == pInfo.Name);

        if (user != null)
        {
            AddNotice(string.Format("Banning and kicking {0} from the game...".L10N("UI:Main:BanAndKickPlayer"), pInfo.Name));
            channel.SendBanMessage(user.Hostname, 8);
            channel.SendKickMessage(user.Name, 8);
        }
    }

    private void HandleCheatDetectedMessage(string sender) =>
        AddNotice(string.Format("{0} has modified game files during the client session. They are likely attempting to cheat!".L10N("UI:Main:PlayerModifyFileCheat"), sender), Color.Red);

    private void HandleTunnelServerChangeMessage(string sender, string tunnelAddressAndPort)
    {
        if (sender != hostName)
            return;

        string[] split = tunnelAddressAndPort.Split(':');
        string tunnelAddress = split[0];
        int tunnelPort = int.Parse(split[1]);

        CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort);
        if (tunnel == null)
        {
            AddNotice(
                ("The game host has selected an invalid tunnel server! " +
                "The game host needs to change the server or you will be unable " +
                "to participate in the match.").L10N("UI:Main:HostInvalidTunnel"),
                Color.Yellow);
            btnLaunchGame.AllowClick = false;
            return;
        }

        HandleTunnelServerChange(tunnel);
        btnLaunchGame.AllowClick = true;
    }

    /// <summary>
    /// Changes the tunnel server used for the game.
    /// </summary>
    /// <param name="tunnel">The new tunnel server to use.</param>
    private void HandleTunnelServerChange(CnCNetTunnel tunnel)
    {
        tunnelHandler.CurrentTunnel = tunnel;
        AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("UI:Main:HostChangeTunnel"), tunnel.Name));
        UpdatePing();
    }

    private void MapSharer_MapDownloadFailed(object sender, SHA1EventArgs e)
        => WindowManager.AddCallback(new Action<SHA1EventArgs>(MapSharer_HandleMapDownloadFailed), e);

    private void MapSharer_HandleMapDownloadFailed(SHA1EventArgs e)
    {
        // If the host has already uploaded the map, we shouldn't request them to re-upload it
        if (hostUploadedMaps.Contains(e.SHA1))
        {
            AddNotice("Download of the custom map failed. The host needs to change the map or you will be unable to participate in this match.".L10N("UI:Main:DownloadCustomMapFailed"));
            mapSharingConfirmationPanel.SetFailedStatus();

            channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + e.SHA1, QueuedMessageType.SYSTEMMESSAGE, 9);
            return;
        }

        AddNotice("Requesting the game host to upload the map to the CnCNet map database.".L10N("UI:Main:RequestHostUploadMapToDB"));

        channel.SendCTCPMessage(MAP_SHARING_UPLOAD_REQUEST + " " + e.SHA1, QueuedMessageType.SYSTEMMESSAGE, 9);
    }

    private void MapSharer_MapDownloadComplete(object sender, SHA1EventArgs e) =>
        WindowManager.AddCallback(new Action<SHA1EventArgs>(MapSharer_HandleMapDownloadComplete), e);

    private void MapSharer_HandleMapDownloadComplete(SHA1EventArgs e)
    {
        string mapFileName = MapSharer.GetMapFileName(e.SHA1, e.MapName);
        Logger.Log("Map " + mapFileName + " downloaded, parsing.");
        string mapPath = "Maps/Custom/" + mapFileName;
        Map map = mapLoader.LoadCustomMap(mapPath, out string returnMessage);
        if (map != null)
        {
            AddNotice(returnMessage);
            if (lastMapSHA1 == e.SHA1)
            {
                GameModeMap = GameModeMaps.Find(gmm => gmm.Map.SHA1 == lastMapSHA1);
                ChangeMap(GameModeMap);
            }
        }
        else
        {
            AddNotice(returnMessage, Color.Red);
            AddNotice("Transfer of the custom map failed. The host needs to change the map or you will be unable to participate in this match.".L10N("UI:Main:MapTransferFailed"));
            mapSharingConfirmationPanel.SetFailedStatus();
            channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + e.SHA1, QueuedMessageType.SYSTEMMESSAGE, 9);
        }
    }

    private void MapSharer_MapUploadFailed(object sender, MapEventArgs e) =>
        WindowManager.AddCallback(new Action<MapEventArgs>(MapSharer_HandleMapUploadFailed), e);

    private void MapSharer_HandleMapUploadFailed(MapEventArgs e)
    {
        Map map = e.Map;

        hostUploadedMaps.Add(map.SHA1);

        AddNotice(string.Format("Uploading map {0} to the CnCNet map database failed.".L10N("UI:Main:UpdateMapToDBFailed"), map.Name));
        if (map == Map)
        {
            AddNotice("You need to change the map or some players won't be able to participate in this match.".L10N("UI:Main:YouMustReplaceMap"));
            channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + map.SHA1, QueuedMessageType.SYSTEMMESSAGE, 9);
        }
    }

    private void MapSharer_MapUploadComplete(object sender, MapEventArgs e) =>
        WindowManager.AddCallback(new Action<MapEventArgs>(MapSharer_HandleMapUploadComplete), e);

    private void MapSharer_HandleMapUploadComplete(MapEventArgs e)
    {
        hostUploadedMaps.Add(e.Map.SHA1);

        AddNotice(string.Format("Uploading map {0} to the CnCNet map database complete.".L10N("UI:Main:UpdateMapToDBSuccess"), e.Map.Name));
        if (e.Map == Map)
        {
            channel.SendCTCPMessage(MAP_SHARING_DOWNLOAD_REQUEST + " " + Map.SHA1, QueuedMessageType.SYSTEMMESSAGE, 9);
        }
    }

    /// <summary>
    /// Handles a map upload request sent by a player.
    /// </summary>
    /// <param name="sender">The sender of the request.</param>
    /// <param name="mapSHA1">The SHA1 of the requested map.</param>
    private void HandleMapUploadRequest(string sender, string mapSHA1)
    {
        if (hostUploadedMaps.Contains(mapSHA1))
        {
            Logger.Log("HandleMapUploadRequest: Map " + mapSHA1 + " is already uploaded!");
            return;
        }

        Map map = null;

        foreach (GameMode gm in GameModeMaps.GameModes)
        {
            map = gm.Maps.Find(m => m.SHA1 == mapSHA1);

            if (map != null)
                break;
        }

        if (map == null)
        {
            Logger.Log("Unknown map upload request from " + sender + ": " + mapSHA1);
            return;
        }

        if (map.Official)
        {
            Logger.Log("HandleMapUploadRequest: Map is official, so skip request");

            AddNotice(string.Format(
                ("{0} doesn't have the map '{1}' on their local installation. " +
                "The map needs to be changed or {0} is unable to participate in the match.").L10N("UI:Main:PlayerMissingMap"),
                sender, map.Name));

            return;
        }

        if (!isHost)
            return;

        AddNotice(string.Format(
            ("{0} doesn't have the map '{1}' on their local installation. " +
            "Attempting to upload the map to the CnCNet map database.").L10N("UI:Main:UpdateMapToDBPrompt"),
            sender, map.Name));

        MapSharer.UploadMap(map, localGame);
    }

    /// <summary>
    /// Handles a map transfer failure message sent by either the player or the game host.
    /// </summary>
    private void HandleMapTransferFailMessage(string sender, string sha1)
    {
        if (sender == hostName)
        {
            AddNotice("The game host failed to upload the map to the CnCNet map database.".L10N("UI:Main:HostUpdateMapToDBFailed"));

            hostUploadedMaps.Add(sha1);

            if (lastMapSHA1 == sha1 && Map == null)
            {
                AddNotice("The game host needs to change the map or you won't be able to participate in this match.".L10N("UI:Main:HostMustChangeMap"));
            }

            return;
        }

        if (lastMapSHA1 == sha1)
        {
            if (!isHost)
            {
                AddNotice(string.Format(
                    "{0} has failed to download the map from the CnCNet map database.".L10N("UI:Main:PlayerDownloadMapFailed") + " " +
                    "The host needs to change the map or {0} won't be able to participate in this match.".L10N("UI:Main:HostNeedChangeMapForPlayer"), sender));
            }
            else
            {
                AddNotice(string.Format(
                    "{0} has failed to download the map from the CnCNet map database.".L10N("UI:Main:PlayerDownloadMapFailed") + " " +
                    "You need to change the map or {0} won't be able to participate in this match.".L10N("UI:Main:YouNeedChangeMapForPlayer"), sender));
            }
        }
    }

    private void HandleMapDownloadRequest(string sender, string sha1)
    {
        if (sender != hostName)
            return;

        hostUploadedMaps.Add(sha1);

        if (lastMapSHA1 == sha1 && Map == null)
        {
            Logger.Log("The game host has uploaded the map into the database. Re-attempting download...");
            MapSharer.DownloadMap(sha1, localGame, lastMapName);
        }
    }

    private void HandleMapSharingBlockedMessage(string sender)
    {
        AddNotice(string.Format(
            "The selected map doesn't exist on {0}'s installation, and they " +
            "have map sharing disabled in settings. The game host needs to change to a non-custom map or " +
            "they will be unable to participate in this match.".L10N("UI:Main:PlayerMissingMaDisabledSharing"), sender));
    }

    /// <summary>
    /// Lowers the time until the next game broadcasting message.
    /// </summary>
    private void AccelerateGameBroadcasting() =>
        gameBroadcastTimer.Accelerate(TimeSpan.FromSeconds(GAME_BROADCAST_ACCELERATION));

    private void BroadcastGame()
    {
        Channel broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

        if (broadcastChannel == null)
            return;

        if (ProgramConstants.IsInGame && broadcastChannel.Users.Count > 500)
            return;

        if (GameMode == null || Map == null)
            return;

        StringBuilder sb = new("GAME ");
        _ = sb.Append(ProgramConstants.CNCNETPROTOCOLREVISION);
        _ = sb.Append(';');
        _ = sb.Append(ProgramConstants.GAME_VERSION);
        _ = sb.Append(';');
        _ = sb.Append(playerLimit);
        _ = sb.Append(';');
        _ = sb.Append(channel.ChannelName);
        _ = sb.Append(';');
        _ = sb.Append(channel.UIName);
        _ = sb.Append(';');
        _ = Locked ? sb.Append('1') : sb.Append('0');
        _ = sb.Append(Convert.ToInt32(isCustomPassword));
        _ = sb.Append(Convert.ToInt32(closed));
        _ = sb.Append('0'); // IsLoadedGame
        _ = sb.Append('0'); // IsLadder
        _ = sb.Append(';');
        foreach (PlayerInfo pInfo in players)
        {
            _ = sb.Append(pInfo.Name);
            _ = sb.Append(',');
        }

        _ = sb.Remove(sb.Length - 1, 1);
        _ = sb.Append(';');
        _ = sb.Append(Map.Name);
        _ = sb.Append(';');
        _ = sb.Append(GameMode.UIName);
        _ = sb.Append(';');
        _ = sb.Append(tunnelHandler.CurrentTunnel.Address + ":" + tunnelHandler.CurrentTunnel.Port);
        _ = sb.Append(';');
        _ = sb.Append(0); // LoadedGameId

        broadcastChannel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEMMESSAGE, 20);
    }

}