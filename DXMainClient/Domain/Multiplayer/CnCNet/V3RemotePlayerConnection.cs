using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;
#if NETFRAMEWORK
using System.Runtime.InteropServices;
using ClientCore.Extensions;
#endif

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Manages a player connection between a remote host and this application.
/// </summary>
internal sealed class V3RemotePlayerConnection : PlayerConnection
{
    private ushort localPort;

    protected override int GameStartReceiveTimeout => 1200000;

    protected override int GameInProgressReceiveTimeout => 1200000;

    public void SetUp(IPAddress remoteIpAddress, ushort remotePort, ushort localPort, uint gameLocalPlayerId, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        PlayerId = gameLocalPlayerId;
#if !NETFRAMEWORK
        RemoteSocketAddress = new IPEndPoint(remoteIpAddress, remotePort).Serialize();
#else
        RemoteEndPoint = new(remoteIpAddress, remotePort);
#endif
        this.localPort = localPort;
    }

    /// <summary>
    /// Occurs when the connection to the remote host succeeded.
    /// </summary>
    public event EventHandler RaiseConnectedEvent;

    /// <summary>
    /// Occurs when the connection to the remote host could not be made.
    /// </summary>
    public event EventHandler RaiseConnectionFailedEvent;

    /// <summary>
    /// Sends local game player data to the remote host.
    /// </summary>
    /// <param name="data">The data to send to the game.</param>
    /// <param name="receiverId">The id of the player that receives the data.</param>
    public ValueTask SendDataToRemotePlayerAsync(Memory<byte> data, uint receiverId)
    {
#if NETFRAMEWORK
        byte[] bytes = BitConverter.GetBytes(PlayerId);

        for (int i = 0; i < PlayerIdSize; i++)
        {
            data.Span[i] = bytes[i];
        }

        bytes = BitConverter.GetBytes(receiverId);

        for (int i = PlayerIdSize; i < PlayerIdSize * 2; i++)
        {
            data.Span[i] = bytes[i - PlayerIdSize];
        }
#else
        if (!BitConverter.TryWriteBytes(data.Span[..PlayerIdSize], PlayerId))
            throw new GameDataException();

        if (!BitConverter.TryWriteBytes(data.Span[PlayerIdSize..(PlayerIdSize * 2)], receiverId))
            throw new GameDataException();
#endif

        return SendDataAsync(data);
    }

    protected override async ValueTask DoStartConnectionAsync()
    {
#if DEBUG
        Logger.Log($"{GetType().Name}: Attempting to establish a connection from port {localPort} to {RemoteEndPoint}).");
#else
        Logger.Log($"{GetType().Name}: Attempting to establish a connection on port {localPort}.");
#endif

#if !NETFRAMEWORK
        Socket = new(RemoteSocketAddress.Family, SocketType.Dgram, ProtocolType.Udp);

        Socket.Bind(new IPEndPoint(RemoteSocketAddress.Family is AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, localPort));
#else
        Socket = new(RemoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        Socket.Bind(new IPEndPoint(RemoteEndPoint.AddressFamily is AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, localPort));
#endif

        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(MaximumPacketSize);
        Memory<byte> buffer = memoryOwner.Memory[..MaximumPacketSize];

        buffer.Span.Clear();

#if NETFRAMEWORK
        byte[] bytes = BitConverter.GetBytes(PlayerId);

        for (int i = 0; i < PlayerIdSize; i++)
        {
            buffer.Span[i] = bytes[i];
        }
#else
        if (!BitConverter.TryWriteBytes(buffer.Span[..PlayerIdSize], PlayerId))
            throw new GameDataException();
#endif

        using var timeoutCancellationTokenSource = new CancellationTokenSource(SendTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, CancellationToken);

        try
        {
#if NETFRAMEWORK
            if (!MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> buffer1))
                throw new();

            await Socket.SendToAsync(buffer1, SocketFlags.None, RemoteEndPoint).WithCancellation(linkedCancellationTokenSource.Token).ConfigureAwait(false);
#else
            await Socket.SendToAsync(buffer, SocketFlags.None, RemoteSocketAddress, linkedCancellationTokenSource.Token).ConfigureAwait(false);
#endif
        }
        catch (SocketException ex)
        {
#if DEBUG
            ProgramConstants.LogException(ex, $"Failed to establish connection from port {localPort} to {RemoteEndPoint}.");
#else
            ProgramConstants.LogException(ex, $"Failed to establish connection on port {localPort}.");
#endif
            OnRaiseConnectionFailedEvent(EventArgs.Empty);

            return;
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (OperationCanceledException)
        {
#if DEBUG
            Logger.Log($"{GetType().Name}: Failed to establish connection (time out) from port {localPort} to {RemoteEndPoint}.");
#else
            Logger.Log($"{GetType().Name}: Failed to establish connection (time out) on port {localPort}.");
#endif
            OnRaiseConnectionFailedEvent(EventArgs.Empty);

            return;
        }

#if DEBUG
        Logger.Log($"{GetType().Name}: Connection from {Socket.LocalEndPoint} to {RemoteEndPoint} established.");
#else
        Logger.Log($"{GetType().Name}: Connection on port {localPort} established.");
#endif
        OnRaiseConnectedEvent(EventArgs.Empty);
    }

#if !NETFRAMEWORK
    protected override ValueTask<int> DoReceiveDataAsync(Memory<byte> buffer, CancellationToken cancellation)
#else
    protected override async ValueTask<int> DoReceiveDataAsync(Memory<byte> buffer, CancellationToken cancellation)
#endif
#if NETFRAMEWORK
    {
        if (!MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> buffer1))
            throw new();

        SocketReceiveFromResult socketReceiveFromResult = await Socket.ReceiveFromAsync(buffer1, SocketFlags.None, RemoteEndPoint).WithCancellation(cancellation).ConfigureAwait(false);

        RemoteEndPoint = (IPEndPoint)socketReceiveFromResult.RemoteEndPoint;

        return socketReceiveFromResult.ReceivedBytes;
    }
#else
        => Socket.ReceiveFromAsync(buffer, SocketFlags.None, RemoteSocketAddress, cancellation);
#endif

    protected override DataReceivedEventArgs ProcessReceivedData(Memory<byte> buffer, int bytesReceived)
    {
        if (bytesReceived < PlayerIdsSize)
        {
#if DEBUG
            Logger.Log($"{GetType().Name}: Invalid data packet from {RemoteEndPoint}");
#else
            Logger.Log($"{GetType().Name}: Invalid data packet on port {localPort}");
#endif
            return null;
        }

        Memory<byte> data = buffer[(PlayerIdSize * 2)..bytesReceived];
#if NETFRAMEWORK
        uint senderId = BitConverter.ToUInt32(buffer[..PlayerIdSize].ToArray(), 0);
        uint receiverId = BitConverter.ToUInt32(buffer[PlayerIdSize..(PlayerIdSize * 2)].ToArray(), 0);
#else
        uint senderId = BitConverter.ToUInt32(buffer[..PlayerIdSize].Span);
        uint receiverId = BitConverter.ToUInt32(buffer[PlayerIdSize..(PlayerIdSize * 2)].Span);
#endif

#if DEBUG
        Logger.Log($"{GetType().Name}: Received {senderId} -> {receiverId} from {RemoteEndPoint} on {Socket.LocalEndPoint}.");

#endif
        if (receiverId != PlayerId)
        {
#if DEBUG
            Logger.Log($"{GetType().Name}: Invalid target (received: {receiverId}, expected: {PlayerId}) from {RemoteEndPoint}.");
#else
            Logger.Log($"{GetType().Name}: Invalid target (received: {receiverId}, expected: {PlayerId}) on port {localPort}.");
#endif

            return null;
        }

        return new(senderId, data);
    }

    private void OnRaiseConnectedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseConnectedEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void OnRaiseConnectionFailedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseConnectionFailedEvent;

        raiseEvent?.Invoke(this, e);
    }
}