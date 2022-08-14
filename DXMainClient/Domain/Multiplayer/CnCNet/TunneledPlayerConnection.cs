﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Captures packets sent by an UDP client (the game) to a specific address
    /// and allows forwarding messages back to it.
    /// </summary>
    internal sealed class TunneledPlayerConnection
    {
        private const int Timeout = 60000;

        public TunneledPlayerConnection(uint playerId)
        {
            PlayerID = playerId;
        }

        public delegate void PacketReceivedEventHandler(TunneledPlayerConnection sender, byte[] data);
        public event PacketReceivedEventHandler PacketReceived;

        public int PortNumber { get; private set; }
        public uint PlayerID { get; }

        private bool _aborted;
        private bool Aborted
        {
            get { lock (locker) return _aborted; }
            set { lock (locker) _aborted = value; }
        }

        private Socket socket;
        private EndPoint endPoint;

        private readonly object locker = new object();


        public void Stop()
        {
            Aborted = true;
        }

        /// <summary>
        /// Creates a socket and sets the connection's port number.
        /// </summary>
        public void CreateSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            socket.Bind(endPoint);

            PortNumber = ((IPEndPoint)socket.LocalEndPoint).Port;
        }

        public void Start()
        {
            Thread thread = new Thread(Run);
            thread.Start();
        }

        private void Run()
        {
            socket.ReceiveTimeout = Timeout;
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    if (Aborted)
                        break;

                    int received = socket.ReceiveFrom(buffer, ref endPoint);

                    byte[] data = new byte[received];
                    Array.Copy(buffer, data, received);
                    Array.Clear(buffer, 0, received);
                    PacketReceived?.Invoke(this, data);
                }
            }
            catch (SocketException)
            {
                // Timeout
            }

            lock (locker)
            {
                _aborted = true;
                socket.Close();
            }
        }

        public void SendPacket(byte[] packet)
        {
            lock (locker)
            {
                if (!_aborted)
                    socket.SendTo(packet, endPoint);
            }
        }
    }
}