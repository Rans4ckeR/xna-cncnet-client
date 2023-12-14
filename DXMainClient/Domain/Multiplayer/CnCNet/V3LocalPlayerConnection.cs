using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
#if DEBUG
using Rampastring.Tools;
#endif
#if NETFRAMEWORK
using System.Runtime.InteropServices;
using ClientCore.Extensions;
#endif

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Manages a player connection between the local game and this application.
/// </summary>
internal sealed class V3LocalPlayerConnection : PlayerConnection
{
    private const uint IOC_IN = 0x80000000;
    private const uint IOC_VENDOR = 0x18000000;
    private const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

    private readonly IPEndPoint loopbackEndPoint = new(IPAddress.Loopback, 0);
#if NET8_0_OR_GREATER
    private SocketAddress loopbackSocketAddress;
#endif

    /// <summary>
    /// Creates a local game socket and returns the port.
    /// </summary>
    /// <param name="playerId">The id of the player for which to create the local game socket.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop the connection.</param>
    /// <returns>The port of the created socket.</returns>
    public ushort Setup(uint playerId, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        PlayerId = playerId;
        Socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
#if NET8_0_OR_GREATER
        loopbackSocketAddress = loopbackEndPoint.Serialize();

        var remoteSocketAddress = new SocketAddress(AddressFamily.InterNetwork);

        loopbackSocketAddress.Buffer.CopyTo(remoteSocketAddress.Buffer);

        RemoteSocketAddress = remoteSocketAddress;
#else
        RemoteEndPoint = loopbackEndPoint;
#endif

        // Disable ICMP port not reachable exceptions, happens when the game is still loading and has not yet opened the socket.
#if !NETFRAMEWORK
        if (OperatingSystem.IsWindows())
#endif
            Socket.IOControl(unchecked((int)SIO_UDP_CONNRESET), [0], null);

        Socket.Bind(loopbackEndPoint);

        return (ushort)((IPEndPoint)Socket.LocalEndPoint!).Port;
    }

    /// <summary>
    /// Sends remote player data to the local game.
    /// </summary>
    /// <param name="data">The data to send to the game.</param>
    public ValueTask SendDataToGameAsync(ReadOnlyMemory<byte> data)
    {
#if NET8_0_OR_GREATER
        if (RemoteSocketAddress.Equals(loopbackSocketAddress) || data.Length < PlayerIdsSize)
#else
        if (RemoteEndPoint.Equals(loopbackEndPoint) || data.Length < PlayerIdsSize)
#endif
        {
#if DEBUG
            Logger.Log($"{GetType().Name}: Discarded remote data from {Socket.LocalEndPoint} to {RemoteEndPoint} for player {PlayerId}.");

#endif
            return default;
        }

        return SendDataAsync(data);
    }

#if NET8_0_OR_GREATER
    protected override ValueTask<int> DoReceiveDataAsync(Memory<byte> buffer, CancellationToken cancellation)
#else
    protected override async ValueTask<int> DoReceiveDataAsync(Memory<byte> buffer, CancellationToken cancellation)
#endif
#if NETFRAMEWORK
    {
        if (!MemoryMarshal.TryGetArray(buffer[PlayerIdsSize..], out ArraySegment<byte> buffer1))
            throw new();

        SocketReceiveFromResult socketReceiveFromResult = await Socket.ReceiveFromAsync(buffer1, SocketFlags.None, RemoteEndPoint).WithCancellation(cancellation).ConfigureAwait(false);

        RemoteEndPoint = (IPEndPoint)socketReceiveFromResult.RemoteEndPoint;

        return socketReceiveFromResult.ReceivedBytes;
    }
#elif NET8_0_OR_GREATER
        => Socket.ReceiveFromAsync(buffer[PlayerIdsSize..], SocketFlags.None, RemoteSocketAddress, cancellation);
#else
    {
        SocketReceiveFromResult socketReceiveFromResult = await Socket.ReceiveFromAsync(buffer[PlayerIdsSize..], SocketFlags.None, RemoteEndPoint, cancellation).ConfigureAwait(false);

        RemoteEndPoint = (IPEndPoint)socketReceiveFromResult.RemoteEndPoint;

        return socketReceiveFromResult.ReceivedBytes;
    }
#endif

    protected override DataReceivedEventArgs ProcessReceivedData(Memory<byte> buffer, int bytesReceived)
        => new(PlayerId, buffer[..(PlayerIdsSize + bytesReceived)]);
}