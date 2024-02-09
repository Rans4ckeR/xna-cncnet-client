using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Manages connections between one or more <see cref="V3LocalPlayerConnection"/>s representing local game players and a <see cref="V3RemotePlayerConnection"/> representing a remote host.
/// </summary>
internal sealed class V3GameTunnelHandler : IAsyncDisposable
{
    private readonly Dictionary<uint, V3LocalPlayerConnection> localGameConnections = [];
    private readonly CancellationTokenSource connectionErrorCancellationTokenSource = new();

    private V3RemotePlayerConnection remoteHostConnection;
    private EventHandler<DataReceivedEventArgs> remoteHostConnectionDataReceivedFunc;
    private EventHandler<DataReceivedEventArgs> localGameConnectionDataReceivedFunc;
    private EventHandler remoteHostConnectionConnectionCutFunc;
    private EventHandler localGameConnectionConnectionCutFunc;

    /// <summary>
    /// Occurs when the connection to the remote host succeeded.
    /// </summary>
    public event EventHandler RaiseRemoteHostConnectedEvent;

    /// <summary>
    /// Occurs when the connection to the remote host could not be made.
    /// </summary>
    public event EventHandler RaiseRemoteHostConnectionFailedEvent;

    /// <summary>
    /// Occurs when data from a remote host is received.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs> RaiseRemoteHostDataReceivedEvent;

    /// <summary>
    /// Occurs when data from the local game is received.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs> RaiseLocalGameDataReceivedEvent;

    public bool ConnectSucceeded { get; private set; }

    public void SetUp(IPAddress remoteIpAddress, ushort remotePort, ushort localPort, uint gameLocalPlayerId, CancellationToken cancellationToken)
    {
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            connectionErrorCancellationTokenSource.Token, cancellationToken);

        remoteHostConnection = new();
        remoteHostConnectionDataReceivedFunc = (sender, e) => RemoteHostConnection_DataReceivedAsync(sender, e).HandleTask();
        localGameConnectionDataReceivedFunc = (sender, e) => LocalGameConnection_DataReceivedAsync(sender, e).HandleTask();
        remoteHostConnectionConnectionCutFunc = (_, _) => RemoteHostConnection_ConnectionCutAsync().HandleTask();
        localGameConnectionConnectionCutFunc = (sender, _) => LocalGameConnection_ConnectionCutAsync(sender).HandleTask();

        remoteHostConnection.RaiseConnectedEvent += RemoteHostConnection_Connected;
        remoteHostConnection.RaiseConnectionFailedEvent += RemoteHostConnection_ConnectionFailed;
        remoteHostConnection.RaiseConnectionCutEvent += remoteHostConnectionConnectionCutFunc;
        remoteHostConnection.RaiseDataReceivedEvent += remoteHostConnectionDataReceivedFunc;

        remoteHostConnection.SetUp(remoteIpAddress, remotePort, localPort, gameLocalPlayerId, cancellationToken);
    }

    public IEnumerable<ushort> CreatePlayerConnections(List<uint> playerIds)
    {
        foreach (uint playerId in playerIds)
        {
            var localPlayerConnection = new V3LocalPlayerConnection();

            localPlayerConnection.RaiseConnectionCutEvent += localGameConnectionConnectionCutFunc;
            localPlayerConnection.RaiseDataReceivedEvent += localGameConnectionDataReceivedFunc;

            localGameConnections.Add(playerId, localPlayerConnection);

            yield return localPlayerConnection.Setup(playerId, connectionErrorCancellationTokenSource.Token);
        }
    }

    public void StartPlayerConnections()
    {
        foreach (KeyValuePair<uint, V3LocalPlayerConnection> playerConnection in localGameConnections)
            playerConnection.Value.StartConnectionAsync().HandleTask();
    }

    public void ConnectToTunnel()
        => remoteHostConnection.StartConnectionAsync().HandleTask();

    public async ValueTask DisposeAsync()
    {
        if (!connectionErrorCancellationTokenSource.IsCancellationRequested)
#if !NETFRAMEWORK
            await connectionErrorCancellationTokenSource.CancelAsync().ConfigureAwait(ConfigureAwaitOptions.None);
#else
        {
            connectionErrorCancellationTokenSource.Cancel();
            await default(ValueTask).ConfigureAwait(false);
        }
#endif

        connectionErrorCancellationTokenSource.Dispose();

        foreach (KeyValuePair<uint, V3LocalPlayerConnection> localGamePlayerConnection in localGameConnections)
        {
            localGamePlayerConnection.Value.RaiseConnectionCutEvent -= localGameConnectionConnectionCutFunc;
            localGamePlayerConnection.Value.RaiseDataReceivedEvent -= localGameConnectionDataReceivedFunc;

            localGamePlayerConnection.Value.Dispose();
        }

        localGameConnections.Clear();

        if (remoteHostConnection == null)
            return;

        remoteHostConnection.RaiseConnectedEvent -= RemoteHostConnection_Connected;
        remoteHostConnection.RaiseConnectionFailedEvent -= RemoteHostConnection_ConnectionFailed;
        remoteHostConnection.RaiseConnectionCutEvent -= remoteHostConnectionConnectionCutFunc;
        remoteHostConnection.RaiseDataReceivedEvent -= remoteHostConnectionDataReceivedFunc;

        remoteHostConnection.Dispose();
    }

    private ValueTask LocalGameConnection_ConnectionCutAsync(object sender)
    {
        var localGamePlayerConnection = sender as V3LocalPlayerConnection;

        localGameConnections.Remove(localGameConnections.Single(q => q.Value == localGamePlayerConnection).Key);

        localGamePlayerConnection!.RaiseConnectionCutEvent -= localGameConnectionConnectionCutFunc;
        localGamePlayerConnection.RaiseDataReceivedEvent -= localGameConnectionDataReceivedFunc;
        localGamePlayerConnection.Dispose();

        return localGameConnections.Count is 0 ? DisposeAsync() : default;
    }

    /// <summary>
    /// Forwards local game data to the remote host.
    /// </summary>
    private ValueTask LocalGameConnection_DataReceivedAsync(object sender, DataReceivedEventArgs e)
    {
        OnRaiseLocalGameDataReceivedEvent(sender, e);

        return remoteHostConnection?.SendDataToRemotePlayerAsync(e.GameData, e.PlayerId) ?? default;
    }

    /// <summary>
    /// Forwards remote player data to the local game.
    /// </summary>
    private ValueTask RemoteHostConnection_DataReceivedAsync(object sender, DataReceivedEventArgs e)
    {
        OnRaiseRemoteHostDataReceivedEvent(sender, e);

        V3LocalPlayerConnection v3LocalPlayerConnection = GetLocalPlayerConnection(e.PlayerId);

        return v3LocalPlayerConnection?.SendDataToGameAsync(e.GameData) ?? default;
    }

    private V3LocalPlayerConnection GetLocalPlayerConnection(uint senderId)
        => localGameConnections.GetValueOrDefault(senderId);

    private void RemoteHostConnection_Connected(object sender, EventArgs e)
    {
        ConnectSucceeded = true;

        OnRaiseRemoteHostConnectedEvent(EventArgs.Empty);
    }

    private void RemoteHostConnection_ConnectionFailed(object sender, EventArgs e)
        => OnRaiseRemoteHostConnectionFailedEvent(EventArgs.Empty);

    private void OnRaiseRemoteHostConnectedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseRemoteHostConnectedEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void OnRaiseRemoteHostConnectionFailedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseRemoteHostConnectionFailedEvent;

        raiseEvent?.Invoke(this, e);
    }

    private ValueTask RemoteHostConnection_ConnectionCutAsync()
        => DisposeAsync();

    private void OnRaiseRemoteHostDataReceivedEvent(object sender, DataReceivedEventArgs e)
    {
        EventHandler<DataReceivedEventArgs> raiseEvent = RaiseRemoteHostDataReceivedEvent;

        raiseEvent?.Invoke(sender, e);
    }

    private void OnRaiseLocalGameDataReceivedEvent(object sender, DataReceivedEventArgs e)
    {
        EventHandler<DataReceivedEventArgs> raiseEvent = RaiseLocalGameDataReceivedEvent;

        raiseEvent?.Invoke(sender, e);
    }
}