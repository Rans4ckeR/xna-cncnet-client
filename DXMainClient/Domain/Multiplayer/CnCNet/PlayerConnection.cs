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

internal abstract class PlayerConnection : IDisposable
{
    protected const int PlayerIdSize = sizeof(uint);
    protected const int PlayerIdsSize = PlayerIdSize * 2;
    protected const int SendTimeout = 10000;
    protected const int MaximumPacketSize = 1024;
#if NET8_0_OR_GREATER && DEBUG

    private SocketAddress remoteSocketAddress;

    private IPEndPoint remoteEndPoint;
#endif

    public uint PlayerId { get; protected set; }

    protected CancellationToken CancellationToken { get; set; }

    protected Socket Socket { get; set; }

#if NET8_0_OR_GREATER
    protected SocketAddress RemoteSocketAddress { get; set; }
#if DEBUG

    protected IPEndPoint RemoteEndPoint
    {
        get
        {
            if (RemoteSocketAddress.Equals(remoteSocketAddress))
                return remoteEndPoint;

            remoteEndPoint = (IPEndPoint)new IPEndPoint(0, 0).Create(RemoteSocketAddress);
            remoteSocketAddress = RemoteSocketAddress;

            return remoteEndPoint;
        }
    }
#endif
#else
    protected IPEndPoint RemoteEndPoint { get; set; }
#endif

    protected virtual int GameStartReceiveTimeout => 60000;

    protected virtual int GameInProgressReceiveTimeout => 10000;

    /// <summary>
    /// Occurs when the connection was lost.
    /// </summary>
    public event EventHandler RaiseConnectionCutEvent;

    /// <summary>
    /// Occurs when game data was received.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs> RaiseDataReceivedEvent;

    public void Dispose()
    {
#if DEBUG
        Logger.Log($"{GetType().Name}: Connection to {RemoteEndPoint} closed for player {PlayerId}.");
#else
        Logger.Log($"{GetType().Name}: Connection closed for player {PlayerId}.");
#endif
        Socket?.Close();
    }

    /// <summary>
    /// Starts listening for game data and forwards it.
    /// </summary>
    public async ValueTask StartConnectionAsync()
    {
        await DoStartConnectionAsync().ConfigureAwait(false);
        await ReceiveLoopAsync().ConfigureAwait(false);
    }

    protected virtual ValueTask DoStartConnectionAsync()
        => default;

    protected abstract ValueTask<int> DoReceiveDataAsync(Memory<byte> buffer, CancellationToken cancellation);

    protected abstract DataReceivedEventArgs ProcessReceivedData(Memory<byte> buffer, int bytesReceived);

    protected async ValueTask SendDataAsync(ReadOnlyMemory<byte> data)
    {
        using var timeoutCancellationTokenSource = new CancellationTokenSource(SendTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, CancellationToken);

        try
        {
#if DEBUG
#if NETWORKTRACE
            Logger.Log($"{GetType().Name}: Sending data from {Socket.LocalEndPoint} to {RemoteEndpoint} for player {PlayerId}: {BitConverter.ToString(data.Span.ToArray())}.");
#else
            Logger.Log($"{GetType().Name}: Sending data from {Socket.LocalEndPoint} to {RemoteEndPoint} for player {PlayerId}.");
#endif
#endif
#if NETFRAMEWORK
            if (!MemoryMarshal.TryGetArray(data, out ArraySegment<byte> buffer1))
                throw new();

            await Socket.SendToAsync(buffer1, SocketFlags.None, RemoteEndPoint).WithCancellation(linkedCancellationTokenSource.Token).ConfigureAwait(false);
#elif NET8_0_OR_GREATER
            await Socket.SendToAsync(data, SocketFlags.None, RemoteSocketAddress, linkedCancellationTokenSource.Token).ConfigureAwait(false);
#else
            await Socket.SendToAsync(data, SocketFlags.None, RemoteEndPoint, linkedCancellationTokenSource.Token).ConfigureAwait(false);
#endif
        }
        catch (SocketException ex)
        {
#if DEBUG
            ProgramConstants.LogException(ex, $"Socket exception sending data to {RemoteEndPoint} for player {PlayerId}.");
#else
            ProgramConstants.LogException(ex, $"Socket exception sending data for player {PlayerId}.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
#if DEBUG
            Logger.Log($"{GetType().Name}: Connection from {Socket.LocalEndPoint} to {RemoteEndPoint} timed out for player {PlayerId} when sending data.");
#else
            Logger.Log($"{GetType().Name}: Connection timed out for player {PlayerId} when sending data.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
    }

    private async ValueTask ReceiveLoopAsync()
    {
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(MaximumPacketSize);
        int receiveTimeout = GameStartReceiveTimeout;

#if DEBUG
        Logger.Log($"{GetType().Name}: Start listening for {RemoteEndPoint} on {Socket.LocalEndPoint} for player {PlayerId}.");
#else
        Logger.Log($"{GetType().Name}: Start listening for player {PlayerId}.");
#endif

        while (!CancellationToken.IsCancellationRequested)
        {
            Memory<byte> buffer = memoryOwner.Memory[..MaximumPacketSize];
            int bytesReceived;
            using var timeoutCancellationTokenSource = new CancellationTokenSource(receiveTimeout);
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, CancellationToken);

            try
            {
                bytesReceived = await DoReceiveDataAsync(buffer, linkedCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
#if DEBUG
                ProgramConstants.LogException(ex, $"Socket exception in {RemoteEndPoint} receive loop for player {PlayerId}.");
#else
                ProgramConstants.LogException(ex, $"Socket exception in receive loop for player {PlayerId}.");
#endif
                OnRaiseConnectionCutEvent(EventArgs.Empty);

                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (OperationCanceledException)
            {
#if DEBUG
                Logger.Log($"{GetType().Name}: Connection from {Socket.LocalEndPoint} to {RemoteEndPoint} timed out for player {PlayerId} when receiving data.");
#else
                Logger.Log($"{GetType().Name}: Connection timed out for player {PlayerId} when receiving data.");
#endif
                OnRaiseConnectionCutEvent(EventArgs.Empty);

                return;
            }

            receiveTimeout = GameInProgressReceiveTimeout;

#if DEBUG
#if NETWORKTRACE
            Logger.Log($"{GetType().Name}: Received data from {RemoteEndpoint} on {Socket.LocalEndPoint} for player {PlayerId}: {BitConverter.ToString(buffer.Span.ToArray())}.");
#else
            Logger.Log($"{GetType().Name}: Received data from {RemoteEndPoint} on {Socket.LocalEndPoint} for player {PlayerId}.");
#endif
#endif

            DataReceivedEventArgs dataReceivedEventArgs = ProcessReceivedData(buffer, bytesReceived);

            if (dataReceivedEventArgs is not null)
                OnRaiseDataReceivedEvent(dataReceivedEventArgs);
        }
    }

    private void OnRaiseConnectionCutEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseConnectionCutEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void OnRaiseDataReceivedEvent(DataReceivedEventArgs e)
    {
        EventHandler<DataReceivedEventArgs> raiseEvent = RaiseDataReceivedEvent;

        raiseEvent?.Invoke(this, e);
    }
}