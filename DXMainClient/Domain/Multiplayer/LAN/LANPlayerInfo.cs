using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ClientCore;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.Domain.Multiplayer.LAN;

public class LANPlayerInfo : PlayerInfo
{
    private const double DROP_TIMEOUT = 20.0;
    private const int LAN_PING_TIMEOUT = 1000;
    private const int PORT = 1234;

    private const double SEND_PING_TIMEOUT = 10.0;

    private readonly Encoding encoding;

    private NetworkStream networkStream;

    private string overMessage = string.Empty;

    public LANPlayerInfo(Encoding encoding)
    {
        this.encoding = encoding;
        Port = PORT;
    }

    public event EventHandler ConnectionLost;

    public event EventHandler<NetworkMessageEventArgs> MessageReceived;

    public event EventHandler PlayerPinged;

    public override string IPAddress
    {
        get
        {
            if (TcpClient != null)
                return ((IPEndPoint)TcpClient.Client.RemoteEndPoint).Address.ToString();

            return base.IPAddress;
        }

        set
        {
            base.IPAddress = value;

            //throw new InvalidOperationException("Cannot set LANPlayerInfo's IPAddress!");
        }
    }

    public TcpClient TcpClient { get; private set; }

    public TimeSpan TimeSinceLastReceivedMessage { get; set; }

    public TimeSpan TimeSinceLastSentMessage { get; set; }

    /// <summary>
    /// Sends a message to the player over the network.
    /// </summary>
    /// <param name="message">The message to send.</param>
    public void SendMessage(string message)
    {
        byte[] buffer;

        buffer = encoding.GetBytes(message + ProgramConstants.LANMESSAGESEPARATOR);

        try
        {
            networkStream.Write(buffer, 0, buffer.Length);
            networkStream.Flush();
        }
        catch
        {
            Logger.Log("Sending message to " + ToString() + " failed!");
        }

        TimeSinceLastSentMessage = TimeSpan.Zero;
    }

    public void SetClient(TcpClient client)
    {
        if (TcpClient != null)
            throw new InvalidOperationException("TcpClient has already been set for this LANPlayerInfo!");

        TcpClient = client;
        TcpClient.SendTimeout = 1000;
        networkStream = client.GetStream();
    }

    /// <summary>
    /// Starts receiving messages from the player asynchronously.
    /// </summary>
    public void StartReceiveLoop()
    {
        Thread thread = new(ReceiveMessages);
        thread.Start();
    }

    public override string ToString()
    {
        return Name + " (" + IPAddress + ")";
    }

    /// <summary>
    /// Updates logic timers for the player.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <returns>True if the player is still considered connected, otherwise false.</returns>
    public bool Update(GameTime gameTime)
    {
        TimeSinceLastReceivedMessage += gameTime.ElapsedGameTime;
        TimeSinceLastSentMessage += gameTime.ElapsedGameTime;

        if (TimeSinceLastSentMessage > TimeSpan.FromSeconds(SEND_PING_TIMEOUT)
            || TimeSinceLastReceivedMessage > TimeSpan.FromSeconds(SEND_PING_TIMEOUT))
        {
            SendMessage("PING");
        }

        if (TimeSinceLastReceivedMessage > TimeSpan.FromSeconds(DROP_TIMEOUT))
            return false;

        return true;
    }

    public void UpdatePing(WindowManager wm)
    {
        using Ping p = new();
        try
        {
            PingReply reply = p.Send(System.Net.IPAddress.Parse(IPAddress), LAN_PING_TIMEOUT);
            if (reply.Status == IPStatus.Success)
                Ping = Convert.ToInt32(reply.RoundtripTime);

            wm.AddCallback(PlayerPinged, this, EventArgs.Empty);
        }
        catch (PingException ex)
        {
            Logger.Log($"Caught an exception when pinging {Name} LAN player: {ex.Message}");
        }
    }

    /// <summary>
    /// Receives messages sent by the client, and hands them over to another class via an event.
    /// </summary>
    private void ReceiveMessages()
    {
        byte[] message = new byte[1024];

        string msg = string.Empty;
        NetworkStream ns = TcpClient.GetStream();

        while (true)
        {
            int bytesRead = 0;
            try
            {
                //blocks until a client sends a message
                bytesRead = ns.Read(message, 0, message.Length);
            }
            catch (Exception ex)
            {
                //a socket error has occured
                Logger.Log("Socket error with client " + Name + "; removing. Message: " + ex.Message);
                ConnectionLost?.Invoke(this, EventArgs.Empty);
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
                    MessageReceived?.Invoke(this, new NetworkMessageEventArgs(cmd));
                }

                continue;
            }

            ConnectionLost?.Invoke(this, EventArgs.Empty);
            break;
        }
    }
}