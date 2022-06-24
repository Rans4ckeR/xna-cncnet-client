using System;
using System.Collections.Generic;
using System.Text;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

/// <summary>
/// A game lobby for loading saved CnCNet games.
/// </summary>
public class CnCNetGameLoadingLobby : GameLoadingLobbyBase
{
    private const double GAME_BROADCAST_INTERVAL = 20.0;
    private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;

    private const string NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND = "NPRSNT";
    private const string GET_READY_CTCP_COMMAND = "GTRDY";
    private const string FILE_HASH_CTCP_COMMAND = "FHSH";
    private const string INVALID_FILE_HASH_CTCP_COMMAND = "IHSH";
    private const string TUNNEL_PING_CTCP_COMMAND = "TNLPNG";
    private const string OPTIONS_CTCP_COMMAND = "OP";
    private const string INVALID_SAVED_GAME_INDEX_CTCP_COMMAND = "ISGI";
    private const string START_GAME_CTCP_COMMAND = "START";
    private const string PLAYER_READY_CTCP_COMMAND = "READY";
    private const string CHANGE_TUNNEL_SERVER_MESSAGE = "CHTNL";

    private readonly CommandHandlerBase[] ctcpCommandHandlers;

    public CnCNetGameLoadingLobby(WindowManager windowManager, TopBar topBar,
        CnCNetManager connectionManager, TunnelHandler tunnelHandler,
        List<GameMode> gameModes, GameCollection gameCollection, DiscordHandler discordHandler)
        : base(windowManager, discordHandler)
    {
        this.connectionManager = connectionManager;
        this.tunnelHandler = tunnelHandler;
        this.gameModes = gameModes;
        this.topBar = topBar;
        this.gameCollection = gameCollection;

        ctcpCommandHandlers = new CommandHandlerBase[]
        {
            new NoParamCommandHandler(NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND, HandleNotAllPresentNotification),
            new NoParamCommandHandler(GET_READY_CTCP_COMMAND, HandleGetReadyNotification),
            new StringCommandHandler(FILE_HASH_CTCP_COMMAND, HandleFileHashCommand),
            new StringCommandHandler(INVALID_FILE_HASH_CTCP_COMMAND, HandleCheaterNotification),
            new IntCommandHandler(TUNNEL_PING_CTCP_COMMAND, HandleTunnelPing),
            new StringCommandHandler(OPTIONS_CTCP_COMMAND, HandleOptionsMessage),
            new NoParamCommandHandler(INVALID_SAVED_GAME_INDEX_CTCP_COMMAND, HandleInvalidSaveIndexCommand),
            new StringCommandHandler(START_GAME_CTCP_COMMAND, HandleStartGameCommand),
            new IntCommandHandler(PLAYER_READY_CTCP_COMMAND, HandlePlayerReadyRequest),
            new StringCommandHandler(CHANGE_TUNNEL_SERVER_MESSAGE, HandleTunnelServerChangeMessage)
        };
    }

    private readonly CnCNetManager connectionManager;

    private readonly List<GameMode> gameModes;

    private readonly TunnelHandler tunnelHandler;
    private readonly GameCollection gameCollection;

    private readonly TopBar topBar;

    private TunnelSelectionWindow tunnelSelectionWindow;
    private XNAClientButton btnChangeTunnel;

    private Channel channel;

    private IRCColor chatColor;

    private string hostName;

    private string localGame;

    private string gameFilesHash;

    private XNATimerControl gameBroadcastTimer;

    private bool started;

    private DarkeningPanel dp;

    public override void Initialize()
    {
        dp = new DarkeningPanel(WindowManager);

        //WindowManager.AddAndInitializeControl(dp);

        //dp.AddChildWithoutInitialize(this);

        //dp.Alpha = 0.0f;
        //dp.Hide();
        localGame = ClientConfiguration.Instance.LocalGame;

        base.Initialize();

        connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
        connectionManager.Disconnected += ConnectionManager_Disconnected;

        tunnelSelectionWindow = new TunnelSelectionWindow(WindowManager, tunnelHandler);
        tunnelSelectionWindow.Initialize();
        tunnelSelectionWindow.DrawOrder = 1;
        tunnelSelectionWindow.UpdateOrder = 1;
        DarkeningPanel.AddAndInitializeWithControl(WindowManager, tunnelSelectionWindow);
        tunnelSelectionWindow.CenterOnParent();
        tunnelSelectionWindow.Disable();
        tunnelSelectionWindow.TunnelSelected += TunnelSelectionWindow_TunnelSelected;

        btnChangeTunnel = new XNAClientButton(WindowManager);
        btnChangeTunnel.Name = nameof(btnChangeTunnel);
        btnChangeTunnel.ClientRectangle = new Rectangle(
            btnLeaveGame.Right - btnLeaveGame.Width - 145,
            btnLeaveGame.Y, UIDesignConstants.BUTTONWIDTH133, UIDesignConstants.BUTTONHEIGHT);
        btnChangeTunnel.Text = "Change Tunnel".L10N("UI:Main:ChangeTunnel");
        btnChangeTunnel.LeftClick += BtnChangeTunnel_LeftClick;
        AddChild(btnChangeTunnel);

        gameBroadcastTimer = new XNATimerControl(WindowManager)
        {
            AutoReset = true,
            Interval = TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL),
            Enabled = true
        };
        gameBroadcastTimer.TimeElapsed += GameBroadcastTimer_TimeElapsed;

        WindowManager.AddAndInitializeControl(gameBroadcastTimer);
    }

    /// <summary>
    /// Sets up events and information before joining the channel.
    /// </summary>
    public void SetUp(bool isHost, CnCNetTunnel tunnel, Channel channel,
        string hostName)
    {
        this.channel = channel;
        this.hostName = hostName;

        channel.MessageAdded += Channel_MessageAdded;
        channel.UserAdded += Channel_UserAdded;
        channel.UserLeft += Channel_UserLeft;
        channel.UserQuitIRC += Channel_UserQuitIRC;
        channel.CTCPReceived += Channel_CTCPReceived;

        tunnelHandler.CurrentTunnel = tunnel;
        tunnelHandler.CurrentTunnelPinged += TunnelHandler_CurrentTunnelPinged;

        started = false;

        Refresh(isHost);
    }

    /// <summary>
    /// Clears event subscriptions and leaves the channel.
    /// </summary>
    public void Clear()
    {
        gameBroadcastTimer.Enabled = false;

        if (channel != null)
        {
            // TODO leave channel only if we've joined the channel
            channel.Leave();

            channel.MessageAdded -= Channel_MessageAdded;
            channel.UserAdded -= Channel_UserAdded;
            channel.UserLeft -= Channel_UserLeft;
            channel.UserQuitIRC -= Channel_UserQuitIRC;
            channel.CTCPReceived -= Channel_CTCPReceived;

            connectionManager.RemoveChannel(channel);
        }

        if (Enabled)
        {
            Enabled = false;
            Visible = false;

            base.LeaveGame();
        }

        tunnelHandler.CurrentTunnel = null;
        tunnelHandler.CurrentTunnelPinged -= TunnelHandler_CurrentTunnelPinged;

        topBar.RemovePrimarySwitchable(this);
    }

    /// <summary>
    /// Called when the local user has joined the game channel.
    /// </summary>
    public void OnJoined()
    {
        FileHashCalculator fhc = new();
        fhc.CalculateHashes(gameModes);

        if (isHost)
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} +klnNs {1} {2}", channel.ChannelName,
                channel.Password, sGPlayers.Count),
                QueuedMessageType.SYSTEMMESSAGE, 50));

            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("TOPIC {0} :{1}", channel.ChannelName,
                ProgramConstants.CNCNETPROTOCOLREVISION + ";" + localGame.ToLower()),
                QueuedMessageType.SYSTEMMESSAGE, 50));

            gameFilesHash = fhc.GetCompleteHash();

            gameBroadcastTimer.Enabled = true;
            gameBroadcastTimer.Start();
            gameBroadcastTimer.SetTime(TimeSpan.FromSeconds(INITIAL_GAME_BROADCAST_DELAY));
        }
        else
        {
            channel.SendCTCPMessage(FILE_HASH_CTCP_COMMAND + " " + fhc.GetCompleteHash(), QueuedMessageType.SYSTEMMESSAGE, 10);

            channel.SendCTCPMessage(TUNNEL_PING_CTCP_COMMAND + " " + tunnelHandler.CurrentTunnel.PingInMs, QueuedMessageType.SYSTEMMESSAGE, 10);

            if (tunnelHandler.CurrentTunnel.PingInMs < 0)
                AddNotice(string.Format("{0} - unknown ping to tunnel server.".L10N("UI:Main:PlayerUnknownPing"), ProgramConstants.PLAYERNAME));
            else
                AddNotice(string.Format("{0} - ping to tunnel server: {1} ms".L10N("UI:Main:PlayerPing"), ProgramConstants.PLAYERNAME, tunnelHandler.CurrentTunnel.PingInMs));
        }

        topBar.AddPrimarySwitchable(this);
        topBar.SwitchToPrimary();
        WindowManager.SelectedControl = tbChatInput;
        UpdateDiscordPresence(true);
    }

    public void ChangeChatColor(IRCColor chatColor)
    {
        this.chatColor = chatColor;
        tbChatInput.TextColor = chatColor.XnaColor;
    }

    public override string GetSwitchName() => "Load Game".L10N("UI:Main:LoadGame");

    protected override void AddNotice(string message, Color color) => channel.AddMessage(new ChatMessage(color, message));

    private void BtnChangeTunnel_LeftClick(object sender, EventArgs e) => ShowTunnelSelectionWindow("Select tunnel server:");

    private void GameBroadcastTimer_TimeElapsed(object sender, EventArgs e) => BroadcastGame();

    private void ConnectionManager_Disconnected(object sender, EventArgs e) => Clear();

    private void ConnectionManager_ConnectionLost(object sender, ConnectionLostEventArgs e) => Clear();

    private void TunnelHandler_CurrentTunnelPinged(object sender, EventArgs e)
    {
        // TODO Rampastring pls, review and merge that XNAIndicator PR already
    }

    private void Channel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
    {
        foreach (CommandHandlerBase cmdHandler in ctcpCommandHandlers)
        {
            if (cmdHandler.Handle(e.UserName, e.Message))
                return;
        }

        Logger.Log("Unhandled CTCP command: " + e.Message + " from " + e.UserName);
    }

    private void Channel_UserAdded(object sender, ChannelUserEventArgs e)
    {
        PlayerInfo pInfo = new()
        {
            Name = e.User.IRCUser.Name
        };

        players.Add(pInfo);

        sndJoinSound.Play();

        BroadcastOptions();
        CopyPlayerDataToUI();
        UpdateDiscordPresence();
    }

    private void Channel_UserLeft(object sender, UserNameEventArgs e)
    {
        RemovePlayer(e.UserName);
        UpdateDiscordPresence();
    }

    private void Channel_UserQuitIRC(object sender, UserNameEventArgs e)
    {
        RemovePlayer(e.UserName);
        UpdateDiscordPresence();
    }

    private void RemovePlayer(string playerName)
    {
        int index = players.FindIndex(p => p.Name == playerName);

        if (index == -1)
            return;

        sndLeaveSound.Play();

        players.RemoveAt(index);

        CopyPlayerDataToUI();

        if (!isHost && playerName == hostName && !ProgramConstants.IsInGame)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(
                Color.Yellow, "The game host left the game!".L10N("UI:Main:HostLeft")));

            Clear();
        }
    }

    private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
    {
        lbChatMessages.AddMessage(e.Message);

        if (e.Message.SenderName != null)
            sndMessageSound.Play();
    }

    protected override void BroadcastOptions()
    {
        if (!isHost)
            return;

        //if (Players.Count > 0)
        players[0].Ready = true;

        StringBuilder message = new(OPTIONS_CTCP_COMMAND + " ");
        _ = message.Append(ddSavedGame.SelectedIndex);
        _ = message.Append(';');
        foreach (PlayerInfo pInfo in players)
        {
            _ = message.Append(pInfo.Name);
            _ = message.Append(':');
            _ = message.Append(Convert.ToInt32(pInfo.Ready));
            _ = message.Append(';');
        }

        _ = message.Remove(message.Length - 1, 1);

        channel.SendCTCPMessage(message.ToString(), QueuedMessageType.GAMESETTINGSMESSAGE, 10);
    }

    protected override void SendChatMessage(string message)
    {
        sndMessageSound.Play();

        channel.SendChatMessage(message, chatColor);
    }

    protected override void RequestReadyStatus() =>
        channel.SendCTCPMessage(PLAYER_READY_CTCP_COMMAND + " 1", QueuedMessageType.GAMEPLAYERSREADYSTATUSMESSAGE, 10);

    protected override void GetReadyNotification()
    {
        base.GetReadyNotification();

        topBar.SwitchToPrimary();

        if (isHost)
            channel.SendCTCPMessage(GET_READY_CTCP_COMMAND, QueuedMessageType.GAMEGETREADYMESSAGE, 0);
    }

    protected override void NotAllPresentNotification()
    {
        base.NotAllPresentNotification();

        if (isHost)
        {
            channel.SendCTCPMessage(
                NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND,
                QueuedMessageType.GAMENOTIFICATIONMESSAGE, 0);
        }
    }

    protected override void HostStartGame()
    {
        AddNotice("Contacting tunnel server...".L10N("UI:Main:ConnectingTunnel"));
        List<int> playerPorts = tunnelHandler.CurrentTunnel.GetPlayerPortInfo(sGPlayers.Count);

        if (playerPorts.Count < players.Count)
        {
            ShowTunnelSelectionWindow(("An error occured while contacting " +
                    "the CnCNet tunnel server." + Environment.NewLine +
                    "Try picking a different tunnel server:").L10N("UI:Main:ConnectTunnelError1"));
            AddNotice(
                ("An error occured while contacting the specified CnCNet " +
                "tunnel server. Please try using a different tunnel server ").L10N("UI:Main:ConnectTunnelError2"), Color.Yellow);
            return;
        }

        StringBuilder sb = new(START_GAME_CTCP_COMMAND + " ");
        for (int pId = 0; pId < players.Count; pId++)
        {
            players[pId].Port = playerPorts[pId];
            _ = sb.Append(players[pId].Name);
            _ = sb.Append(';');
            _ = sb.Append("0.0.0.0:");
            _ = sb.Append(playerPorts[pId]);
            _ = sb.Append(';');
        }

        _ = sb.Remove(sb.Length - 1, 1);
        channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEMMESSAGE, 9);

        AddNotice("Starting game...".L10N("UI:Main:StartingGame"));

        started = true;

        LoadGame();
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

    private void HandleGetReadyNotification(string sender)
    {
        if (sender != hostName)
            return;

        GetReadyNotification();
    }

    private void HandleNotAllPresentNotification(string sender)
    {
        if (sender != hostName)
            return;

        NotAllPresentNotification();
    }

    private void HandleFileHashCommand(string sender, string fileHash)
    {
        if (!isHost)
            return;

        if (fileHash != gameFilesHash)
        {
            PlayerInfo pInfo = players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            pInfo.Verified = true;

            HandleCheaterNotification(hostName, sender); // This is kinda hacky
        }
    }

    private void HandleCheaterNotification(string sender, string cheaterName)
    {
        if (sender != hostName)
            return;

        AddNotice(string.Format("{0} - modified files detected! They could be cheating!".L10N("UI:Main:PlayerCheating"), cheaterName), Color.Red);

        if (isHost)
            channel.SendCTCPMessage(INVALID_FILE_HASH_CTCP_COMMAND + " " + cheaterName, QueuedMessageType.SYSTEMMESSAGE, 0);
    }

    private void HandleTunnelPing(string sender, int pingInMs)
    {
        if (pingInMs < 0)
            AddNotice(string.Format("{0} - unknown ping to tunnel server.".L10N("UI:Main:PlayerUnknownPing"), sender));
        else
            AddNotice(string.Format("{0} - ping to tunnel server: {1} ms".L10N("UI:Main:PlayerPing"), sender, pingInMs));
    }

    /// <summary>
    /// Handles an options broadcast sent by the game host.
    /// </summary>
    private void HandleOptionsMessage(string sender, string data)
    {
        if (sender != hostName)
            return;

        string[] parts = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 1)
            return;

        int sgIndex = Conversions.IntFromString(parts[0], -1);

        if (sgIndex < 0)
            return;

        if (sgIndex >= ddSavedGame.Items.Count)
        {
            AddNotice("The game host has selected an invalid saved game index!".L10N("UI:Main:HostInvalidIndex") + " " + sgIndex);
            channel.SendCTCPMessage(INVALID_SAVED_GAME_INDEX_CTCP_COMMAND, QueuedMessageType.SYSTEMMESSAGE, 10);
            return;
        }

        ddSavedGame.SelectedIndex = sgIndex;

        players.Clear();

        for (int i = 1; i < parts.Length; i++)
        {
            string[] playerAndReadyStatus = parts[i].Split(':');
            if (playerAndReadyStatus.Length < 2)
                return;

            string playerName = playerAndReadyStatus[0];
            int readyStatus = Conversions.IntFromString(playerAndReadyStatus[1], -1);

            if (string.IsNullOrEmpty(playerName) || readyStatus == -1)
                return;

            PlayerInfo pInfo = new()
            {
                Name = playerName,
                Ready = Convert.ToBoolean(readyStatus)
            };

            players.Add(pInfo);
        }

        CopyPlayerDataToUI();
    }

    private void HandleInvalidSaveIndexCommand(string sender)
    {
        PlayerInfo pInfo = players.Find(p => p.Name == sender);

        if (pInfo == null)
            return;

        pInfo.Ready = false;

        AddNotice(string.Format("{0} does not have the selected saved game on their system! Try selecting an earlier saved game.".L10N("UI:Main:PlayerDontHaveSavedGame"), pInfo.Name));

        CopyPlayerDataToUI();
    }

    private void HandleStartGameCommand(string sender, string data)
    {
        if (sender != hostName)
            return;

        string[] parts = data.Split(';');

        int playerCount = parts.Length / 2;

        for (int i = 0; i < playerCount; i++)
        {
            if (parts.Length < (i * 2) + 1)
                return;

            string pName = parts[i * 2];
            string ipAndPort = parts[(i * 2) + 1];
            string[] ipAndPortSplit = ipAndPort.Split(':');

            if (ipAndPortSplit.Length < 2)
                return;

            bool success = int.TryParse(ipAndPortSplit[1], out int port);
            if (!success)
                return;

            PlayerInfo pInfo = players.Find(p => p.Name == pName);

            if (pInfo == null)
                continue;

            pInfo.Port = port;
        }

        LoadGame();
    }

    private void HandlePlayerReadyRequest(string sender, int readyStatus)
    {
        PlayerInfo pInfo = players.Find(p => p.Name == sender);

        if (pInfo == null)
            return;

        pInfo.Ready = Convert.ToBoolean(readyStatus);

        CopyPlayerDataToUI();

        if (isHost)
            BroadcastOptions();
    }

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
            btnLoadGame.AllowClick = false;
            return;
        }

        HandleTunnelServerChange(tunnel);
        btnLoadGame.AllowClick = true;
    }

    /// <summary>
    /// Changes the tunnel server used for the game.
    /// </summary>
    /// <param name="tunnel">The new tunnel server to use.</param>
    private void HandleTunnelServerChange(CnCNetTunnel tunnel)
    {
        tunnelHandler.CurrentTunnel = tunnel;
        AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("UI:Main:HostChangeTunnel"), tunnel.Name));

        //UpdatePing();
    }

    protected override void WriteSpawnIniAdditions(IniFile spawnIni)
    {
        spawnIni.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
        spawnIni.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);

        base.WriteSpawnIniAdditions(spawnIni);
    }

    protected override void HandleGameProcessExited()
    {
        base.HandleGameProcessExited();

        Clear();
    }

    protected override void LeaveGame() => Clear();

    private void BroadcastGame()
    {
        Channel broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

        if (broadcastChannel == null)
            return;

        StringBuilder sb = new("GAME ");
        _ = sb.Append(ProgramConstants.CNCNETPROTOCOLREVISION);
        _ = sb.Append(';');
        _ = sb.Append(ProgramConstants.GAME_VERSION);
        _ = sb.Append(';');
        _ = sb.Append(sGPlayers.Count);
        _ = sb.Append(';');
        _ = sb.Append(channel.ChannelName);
        _ = sb.Append(';');
        _ = sb.Append(channel.UIName);
        _ = sb.Append(';');
        _ = started || players.Count == sGPlayers.Count ? sb.Append('1') : sb.Append('0');
        _ = sb.Append('0'); // IsCustomPassword
        _ = sb.Append('0'); // Closed
        _ = sb.Append('1'); // IsLoadedGame
        _ = sb.Append('0'); // IsLadder
        _ = sb.Append(';');
        foreach (SavedGamePlayer sgPlayer in sGPlayers)
        {
            _ = sb.Append(sgPlayer.Name);
            _ = sb.Append(',');
        }

        _ = sb.Remove(sb.Length - 1, 1);
        _ = sb.Append(';');
        _ = sb.Append(lblMapNameValue.Text);
        _ = sb.Append(';');
        _ = sb.Append(lblGameModeValue.Text);
        _ = sb.Append(';');
        _ = sb.Append(tunnelHandler.CurrentTunnel.Address + ":" + tunnelHandler.CurrentTunnel.Port);
        _ = sb.Append(';');
        _ = sb.Append(0); // LoadedGameId

        broadcastChannel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEMMESSAGE, 20);
    }

    protected override void UpdateDiscordPresence(bool resetTimer = false)
    {
        if (discordHandler == null)
            return;

        PlayerInfo player = players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
        if (player == null)
            return;
        string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby"; // not UI strings

        discordHandler.UpdatePresence(
            lblMapNameValue.Text, lblGameModeValue.Text, "Multiplayer",
            currentState, players.Count, sGPlayers.Count,
            channel.UIName, isHost, resetTimer);
    }
}