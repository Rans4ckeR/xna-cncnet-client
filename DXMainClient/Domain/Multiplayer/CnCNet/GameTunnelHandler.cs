﻿using System;
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    internal sealed class GameTunnelHandler
    {
        public event EventHandler Connected;
        public event EventHandler ConnectionFailed;

        private uint senderId;

        private V3TunnelConnection tunnelConnection;
        private Dictionary<uint, TunneledPlayerConnection> playerConnections = new();

        private readonly object locker = new();

        public void SetUp(CnCNetTunnel tunnel, uint ourSenderId)
        {
            senderId = ourSenderId;

            tunnelConnection = new V3TunnelConnection(tunnel, senderId);
            tunnelConnection.Connected += TunnelConnection_Connected;
            tunnelConnection.ConnectionFailed += TunnelConnection_ConnectionFailed;
            tunnelConnection.ConnectionCut += TunnelConnection_ConnectionCut;
            tunnelConnection.MessageReceived += TunnelConnection_MessageReceived;
        }

        public void ConnectToTunnel()
        {
            if (tunnelConnection == null)
                throw new InvalidOperationException("GameTunnelHandler: Call SetUp before calling ConnectToTunnel.");

            tunnelConnection.ConnectAsync();
        }

        public int[] CreatePlayerConnections(List<uint> playerIds)
        {
            int[] ports = new int[playerIds.Count];
            playerConnections = new Dictionary<uint, TunneledPlayerConnection>();

            for (int i = 0; i < playerIds.Count; i++)
            {
                var playerConnection = new TunneledPlayerConnection(playerIds[i]);
                playerConnection.CreateSocket();
                ports[i] = playerConnection.PortNumber;
                playerConnections.Add(playerIds[i], playerConnection);
                playerConnection.PacketReceived += PlayerConnection_PacketReceived;
                playerConnection.Start();
            }

            return ports;
        }

        public void Clear()
        {
            lock (locker)
            {
                foreach (var connection in playerConnections)
                {
                    connection.Value.Stop();
                    connection.Value.PacketReceived -= PlayerConnection_PacketReceived;
                }

                playerConnections.Clear();

                if (tunnelConnection == null)
                    return;

                tunnelConnection.CloseConnection();
                tunnelConnection.Connected -= TunnelConnection_Connected;
                tunnelConnection.ConnectionFailed -= TunnelConnection_ConnectionFailed;
                tunnelConnection.ConnectionCut -= TunnelConnection_ConnectionCut;
                tunnelConnection.MessageReceived -= TunnelConnection_MessageReceived;
                tunnelConnection = null;
            }
        }

        private void PlayerConnection_PacketReceived(TunneledPlayerConnection sender, byte[] data)
        {
            lock (locker)
            {
                if (tunnelConnection != null)
                    tunnelConnection.SendData(data, sender.PlayerID);
            }
        }

        private void TunnelConnection_MessageReceived(byte[] data, uint senderId)
        {
            lock (locker)
            {
                if (playerConnections.TryGetValue(senderId, out TunneledPlayerConnection connection))
                    connection.SendPacket(data);
            }
        }

        private void TunnelConnection_Connected(object sender, EventArgs e)
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        private void TunnelConnection_ConnectionFailed(object sender, EventArgs e)
        {
            ConnectionFailed?.Invoke(this, EventArgs.Empty);
            Clear();
        }

        private void TunnelConnection_ConnectionCut(object sender, EventArgs e)
        {
            Clear();
        }
    }
}