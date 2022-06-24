using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ClientCore;
using DTAClient.Domain;
using DTAClient.Domain.LAN;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.LAN;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer;

internal class LANGameLoadingLobby : GameLoadingLobbyBase
{
    private const double DROPOUT_TIMEOUT = 20.0;
    private const double GAME_BROADCAST_INTERVAL = 10.0;

    private const string OPTIONS_COMMAND = "OPTS";
    private const string GAME_LAUNCH_COMMAND = "START";
    private const string READY_STATUS_COMMAND = "READY";
    private const string CHAT_COMMAND = "CHAT";
    private const string PLAYER_QUIT_COMMAND = "QUIT";
    private const string PLAYER_JOIN_COMMAND = "JOIN";
    private const string FILE_HASH_COMMAND = "FHASH";
    private readonly LANColor[] chatColors;
    private readonly Encoding encoding;

    private TcpListener listener;

    public LANGameLoadingLobby(
        WindowManager windowManager,
        List<GameMode> gameModes, LANColor[] chatColors, DiscordHandler discordHandler)
        : base(windowManager, discordHandler)
    {
        encoding = ProgramConstants.LANENCODING;
        this.gameModes = gameModes;
        this.chatColors = chatColors;

        localGame = ClientConfiguration.Instance.LocalGame;

        hostCommandHandlers = new LANServerCommandHandler[]
        {
            new ServerStringCommandHandler(CHAT_COMMAND, Server_HandleChatMessage),
            new ServerStringCommandHandler(FILE_HASH_COMMAND, Server_HandleFileHashMessage),
            new ServerNoParamCommandHandler(READY_STATUS_COMMAND, Server_HandleReadyRequest),
        };

        playerCommandHandlers = new LANClientCommandHandler[]
        {
            new ClientStringCommandHandler(CHAT_COMMAND, Client_HandleChatMessage),
            new ClientStringCommandHandler(OPTIONS_COMMAND, Client_HandleOptionsMessage),
            new ClientNoParamCommandHandler(GAME_LAUNCH_COMMAND, Client_HandleStartCommand)
        };

        WindowManager.GameClosing += WindowManager_GameClosing;
    }

    public event EventHandler<LobbyNotificationEventArgs> LobbyNotification;

    private void WindowManager_GameClosing(object sender, EventArgs e)
    {
        if (client != null && client.Connected)
            Clear();
    }

    public event EventHandler<GameBroadcastEventArgs> GameBroadcast;
    private TcpClient client;

    private IPEndPoint hostEndPoint;
    private int chatColorIndex;

    private readonly LANServerCommandHandler[] hostCommandHandlers;
    private readonly LANClientCommandHandler[] playerCommandHandlers;

    private readonly string localGame;

    private readonly List<GameMode> gameModes;

    private TimeSpan timeSinceGameBroadcast = TimeSpan.Zero;

    private TimeSpan timeSinceLastReceivedCommand = TimeSpan.Zero;

    private string overMessage = string.Empty;

    private string localFileHash;

    private int loadedGameId;

    private bool started = false;

    public void SetUp(
        bool isHost,
        IPEndPoint hostEndPoint, TcpClient client,
        int loadedGameId)
    {
        Refresh(isHost);

        this.hostEndPoint = hostEndPoint;

        this.loadedGameId = loadedGameId;

        started = false;

        if (isHost)
        {
            Thread thread = new(ListenForClients);
            thread.Start();

            this.client = new TcpClient();
            this.client.Connect("127.0.0.1", ProgramConstants.LANGAMELOBBYPORT);

            byte[] buffer = encoding.GetBytes(PLAYER_JOIN_COMMAND +
                ProgramConstants.LANDATASEPARATOR + ProgramConstants.PLAYERNAME +
                ProgramConstants.LANDATASEPARATOR + loadedGameId);

            this.client.GetStream().Write(buffer, 0, buffer.Length);
            this.client.GetStream().Flush();

            FileHashCalculator fhc = new();
            fhc.CalculateHashes(gameModes);
            localFileHash = fhc.GetCompleteHash();
        }
        else
        {
            this.client = client;
        }

        new Thread(HandleServerCommunication).Start();

        if (base.isHost)
            CopyPlayerDataToUI();

        WindowManager.SelectedControl = tbChatInput;
    }

    public void PostJoin()
    {
        FileHashCalculator fhc = new();
        fhc.CalculateHashes(gameModes);
        SendMessageToHost(FILE_HASH_COMMAND + " " + fhc.GetCompleteHash());
        UpdateDiscordPresence(true);
    }

    public void SetChatColorIndex(int colorIndex)
    {
        chatColorIndex = colorIndex;
    }

    protected override void LeaveGame()
    {
        Clear();
        Disable();

        base.LeaveGame();
    }

    protected override void AddNotice(string message, Color color)
    {
        lbChatMessages.AddMessage(null, message, color);
    }

    #region Server code

    private void ListenForClients()
    {
        listener = new TcpListener(IPAddress.Any, ProgramConstants.LANGAMELOBBYPORT);
        listener.Start();

        while (true)
        {
            TcpClient client;

            try
            {
                client = listener.AcceptTcpClient();
            }
            catch (Exception ex)
            {
                Logger.Log("Listener error: " + ex.Message);
                break;
            }

            Logger.Log("New client connected from " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());

            LANPlayerInfo lpInfo = new(encoding);
            lpInfo.SetClient(client);

            Thread thread = new(new ParameterizedThreadStart(HandleClientConnection));
            thread.Start(lpInfo);
        }
    }

    private void HandleClientConnection(object clientInfo)
    {
        LANPlayerInfo lpInfo = (LANPlayerInfo)clientInfo;

        byte[] message = new byte[1024];

        while (true)
        {
            int bytesRead = 0;

            try
            {
                bytesRead = lpInfo.TcpClient.GetStream().Read(message, 0, message.Length);
            }
            catch (Exception ex)
            {
                Logger.Log("Socket error with client " + lpInfo.IPAddress + "; removing. Message: " + ex.Message);
                break;
            }

            if (bytesRead == 0)
            {
                Logger.Log("Connect attempt from " + lpInfo.IPAddress + " failed! (0 bytes read)");

                break;
            }

            string msg = encoding.GetString(message, 0, bytesRead);

            string[] command = msg.Split(ProgramConstants.LANMESSAGESEPARATOR);
            string[] parts = command[0].Split(ProgramConstants.LANDATASEPARATOR);

            if (parts.Length != 3)
                break;

            string name = parts[1].Trim();
            int loadedGameId = Conversions.IntFromString(parts[2], -1);

            if (parts[0] == "JOIN" && !string.IsNullOrEmpty(name)
                && loadedGameId == this.loadedGameId)
            {
                lpInfo.Name = name;

                AddCallback(new Action<LANPlayerInfo>(AddPlayer), lpInfo);
                return;
            }

            break;
        }

        if (lpInfo.TcpClient.Connected)
            lpInfo.TcpClient.Close();
    }

    private void AddPlayer(LANPlayerInfo lpInfo)
    {
        if (players.Find(p => p.Name == lpInfo.Name) != null ||
            players.Count >= sGPlayers.Count ||
            sGPlayers.Find(p => p.Name == lpInfo.Name) == null)
        {
            lpInfo.TcpClient.Close();
            return;
        }

        if (players.Count == 0)
            lpInfo.Ready = true;

        players.Add(lpInfo);

        lpInfo.MessageReceived += LpInfo_MessageReceived;
        lpInfo.ConnectionLost += LpInfo_ConnectionLost;

        sndJoinSound.Play();

        AddNotice(string.Format("{0} connected from {1}".L10N("UI:Main:PlayerFromIP"), lpInfo.Name, lpInfo.IPAddress));
        lpInfo.StartReceiveLoop();

        CopyPlayerDataToUI();
        BroadcastOptions();
        UpdateDiscordPresence();
    }

    private void LpInfo_ConnectionLost(object sender, EventArgs e)
    {
        LANPlayerInfo lpInfo = (LANPlayerInfo)sender;
        CleanUpPlayer(lpInfo);
        _ = players.Remove(lpInfo);

        AddNotice(string.Format("{0} has left the game.".L10N("UI:Main:PlayerLeftGame"), lpInfo.Name));

        sndLeaveSound.Play();

        CopyPlayerDataToUI();
        BroadcastOptions();
        UpdateDiscordPresence();
    }

    private void LpInfo_MessageReceived(object sender, NetworkMessageEventArgs e)
    {
        AddCallback(
            new Action<string, LANPlayerInfo>(HandleClientMessage),
            e.Message, (LANPlayerInfo)sender);
    }

    private void HandleClientMessage(string data, LANPlayerInfo lpInfo)
    {
        lpInfo.TimeSinceLastReceivedMessage = TimeSpan.Zero;

        foreach (LANServerCommandHandler cmdHandler in hostCommandHandlers)
        {
            if (cmdHandler.Handle(lpInfo, data))
                return;
        }

        Logger.Log("Unknown LAN command from " + lpInfo.ToString() + " : " + data);
    }

    private void CleanUpPlayer(LANPlayerInfo lpInfo)
    {
        lpInfo.MessageReceived -= LpInfo_MessageReceived;
        lpInfo.TcpClient.Close();
    }

    #endregion

    private void HandleServerCommunication()
    {
        byte[] message = new byte[1024];

        string msg = string.Empty;

        int bytesRead = 0;

        if (!client.Connected)
            return;

        NetworkStream stream = client.GetStream();

        while (true)
        {
            bytesRead = 0;

            try
            {
                bytesRead = stream.Read(message, 0, message.Length);
            }
            catch (Exception ex)
            {
                Logger.Log("Reading data from the server failed! Message: " + ex.Message);
                LeaveGame();
                break;
            }

            if (bytesRead > 0)
            {
                msg = encoding.GetString(message, 0, bytesRead);

                msg = overMessage + msg;
                List<string> commands = new();

                while (true)
                {
                    int index = msg.IndexOf(ProgramConstants.LANMESSAGESEPARATOR);

                    if (index == -1)
                    {
                        overMessage = msg;
                        break;
                    }
                    else
                    {
                        commands.Add(msg.Substring(0, index));
                        msg = msg.Substring(index + 1);
                    }
                }

                foreach (string cmd in commands)
                {
                    AddCallback(new Action<string>(HandleMessageFromServer), cmd);
                }

                continue;
            }

            Logger.Log("Reading data from the server failed (0 bytes received)!");
            LeaveGame();
            break;
        }
    }

    private void HandleMessageFromServer(string message)
    {
        timeSinceLastReceivedCommand = TimeSpan.Zero;

        foreach (LANClientCommandHandler cmdHandler in playerCommandHandlers)
        {
            if (cmdHandler.Handle(message))
                return;
        }

        Logger.Log("Unknown LAN command from the server: " + message);
    }

    private void Clear()
    {
        if (isHost)
        {
            BroadcastMessage(PLAYER_QUIT_COMMAND);
            players.ForEach(p => CleanUpPlayer((LANPlayerInfo)p));
            players.Clear();
            listener.Stop();
        }
        else
        {
            SendMessageToHost(PLAYER_QUIT_COMMAND);
        }

        if (client.Connected)
            client.Close();
    }

    protected override void BroadcastOptions()
    {
        if (players.Count > 0)
            players[0].Ready = true;

        ExtendedStringBuilder sb = new(OPTIONS_COMMAND + " ", true)
        {
            Separator = ProgramConstants.LANDATASEPARATOR
        };

        sb.Append(ddSavedGame.SelectedIndex);

        foreach (PlayerInfo pInfo in players)
        {
            sb.Append(pInfo.Name);
            sb.Append(Convert.ToInt32(pInfo.Ready));
            sb.Append(pInfo.IPAddress);
        }

        BroadcastMessage(sb.ToString());
    }

    protected override void HostStartGame()
    {
        BroadcastMessage(GAME_LAUNCH_COMMAND);
    }

    protected override void RequestReadyStatus()
    {
        SendMessageToHost(READY_STATUS_COMMAND);
    }

    protected override void SendChatMessage(string message)
    {
        SendMessageToHost(CHAT_COMMAND + " " + chatColorIndex +
            ProgramConstants.LANDATASEPARATOR + message);

        sndMessageSound.Play();
    }

    #region Server's command handlers

    private void Server_HandleChatMessage(LANPlayerInfo sender, string data)
    {
        string[] parts = data.Split(ProgramConstants.LANDATASEPARATOR);

        if (parts.Length < 2)
            return;

        int colorIndex = Conversions.IntFromString(parts[0], -1);

        if (colorIndex < 0 || colorIndex >= chatColors.Length)
            return;

        BroadcastMessage(CHAT_COMMAND + " " + sender +
            ProgramConstants.LANDATASEPARATOR + colorIndex +
            ProgramConstants.LANDATASEPARATOR + data);
    }

    private void Server_HandleFileHashMessage(LANPlayerInfo sender, string hash)
    {
        if (hash != localFileHash)
            AddNotice(string.Format("{0} - modified files detected! They could be cheating!".L10N("UI:Main:PlayerCheating"), sender.Name), Color.Red);
        sender.Verified = true;
    }

    private void Server_HandleReadyRequest(LANPlayerInfo sender)
    {
        if (!sender.Ready)
        {
            sender.Ready = true;
            CopyPlayerDataToUI();
            BroadcastOptions();
        }
    }

    #endregion

    #region Client's command handlers

    private void Client_HandleChatMessage(string data)
    {
        string[] parts = data.Split(ProgramConstants.LANDATASEPARATOR);

        if (parts.Length < 3)
            return;

        string playerName = parts[0];

        int colorIndex = Conversions.IntFromString(parts[1], -1);

        if (colorIndex < 0 || colorIndex >= chatColors.Length)
            return;

        lbChatMessages.AddMessage(new ChatMessage(
            playerName,
            chatColors[colorIndex].XNAColor, DateTime.Now, parts[2]));

        sndMessageSound.Play();
    }

    private void Client_HandleOptionsMessage(string data)
    {
        if (isHost)
            return;

        string[] parts = data.Split(ProgramConstants.LANDATASEPARATOR);
        const int PLAYER_INFO_PARTS = 3;
        int pCount = (parts.Length - 1) / PLAYER_INFO_PARTS;

        if ((pCount * PLAYER_INFO_PARTS) + 1 != parts.Length)
            return;

        int savedGameIndex = Conversions.IntFromString(parts[0], -1);
        if (savedGameIndex < 0 || savedGameIndex >= ddSavedGame.Items.Count)
        {
            return;
        }

        ddSavedGame.SelectedIndex = savedGameIndex;

        players.Clear();

        for (int i = 0; i < pCount; i++)
        {
            int baseIndex = 1 + (i * PLAYER_INFO_PARTS);
            string pName = parts[baseIndex];
            bool ready = Conversions.IntFromString(parts[baseIndex + 1], -1) > 0;
            string ipAddress = parts[baseIndex + 2];

            LANPlayerInfo pInfo = new(encoding)
            {
                Name = pName,
                Ready = ready,
                IPAddress = ipAddress
            };
            players.Add(pInfo);
        }

        if (players.Count > 0) // Set IP of host
            players[0].IPAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

        CopyPlayerDataToUI();
    }

    private void Client_HandleStartCommand()
    {
        started = true;

        LoadGame();
    }

    #endregion

    /// <summary>
    /// Broadcasts a command to all players in the game as the game host.
    /// </summary>
    /// <param name="message">The command to send.</param>
    private void BroadcastMessage(string message)
    {
        if (!isHost)
            return;

        foreach (PlayerInfo pInfo in players)
        {
            LANPlayerInfo lpInfo = (LANPlayerInfo)pInfo;
            lpInfo.SendMessage(message);
        }
    }

    private void SendMessageToHost(string message)
    {
        if (!client.Connected)
            return;

        byte[] buffer = encoding.GetBytes(
            message + ProgramConstants.LANMESSAGESEPARATOR);

        NetworkStream ns = client.GetStream();

        try
        {
            ns.Write(buffer, 0, buffer.Length);
            ns.Flush();
        }
        catch
        {
            Logger.Log("Sending message to game host failed!");
        }
    }

    public override string GetSwitchName()
    {
        return "Load Game".L10N("UI:Main:LoadGameSwitchName");
    }

    public override void Update(GameTime gameTime)
    {
        if (isHost)
        {
            for (int i = 1; i < players.Count; i++)
            {
                LANPlayerInfo lpInfo = (LANPlayerInfo)players[i];
                if (!lpInfo.Update(gameTime))
                {
                    CleanUpPlayer(lpInfo);
                    players.RemoveAt(i);
                    AddNotice(string.Format("{0} - connection timed out".L10N("UI:Main:PlayerTimeout"), lpInfo.Name));
                    CopyPlayerDataToUI();
                    BroadcastOptions();
                    UpdateDiscordPresence();
                    i--;
                }
            }

            timeSinceGameBroadcast += gameTime.ElapsedGameTime;

            if (timeSinceGameBroadcast > TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL))
            {
                BroadcastGame();
                timeSinceGameBroadcast = TimeSpan.Zero;
            }
        }
        else
        {
            timeSinceLastReceivedCommand += gameTime.ElapsedGameTime;

            if (timeSinceLastReceivedCommand > TimeSpan.FromSeconds(DROPOUT_TIMEOUT))
            {
                LobbyNotification?.Invoke(
                    this,
                    new LobbyNotificationEventArgs("Connection to the game host timed out.".L10N("UI:Main:HostConnectTimeOut")));
                LeaveGame();
            }
        }

        base.Update(gameTime);
    }

    protected override void HandleGameProcessExited()
    {
        base.HandleGameProcessExited();

        LeaveGame();
    }

    private void BroadcastGame()
    {
        ExtendedStringBuilder sb = new("GAME ", true)
        {
            Separator = ProgramConstants.LANDATASEPARATOR
        };
        sb.Append(ProgramConstants.LANPROTOCOLREVISION);
        sb.Append(ProgramConstants.GAME_VERSION);
        sb.Append(localGame);
        sb.Append(lblMapNameValue.Text);
        sb.Append(lblGameModeValue.Text);
        sb.Append(0); // LoadedGameID
        StringBuilder sbPlayers = new();
        sGPlayers.ForEach(p => sbPlayers.Append(p.Name + ","));
        _ = sbPlayers.Remove(sbPlayers.Length - 1, 1);
        sb.Append(sbPlayers.ToString());
        sb.Append(Convert.ToInt32(started || players.Count == sGPlayers.Count));
        sb.Append(1); // IsLoadedGame

        GameBroadcast?.Invoke(this, new GameBroadcastEventArgs(sb.ToString()));
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
            lblMapNameValue.Text, lblGameModeValue.Text, currentState, "LAN",
            players.Count, sGPlayers.Count,
            "LAN Game", isHost, resetTimer);
    }
}