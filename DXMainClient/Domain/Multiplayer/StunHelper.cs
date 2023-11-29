using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using Rampastring.Tools;
#if NETFRAMEWORK
using System.Runtime.InteropServices;
#endif

namespace DTAClient.Domain.Multiplayer;

internal static class StunHelper
{
    private const int PingTimeout = 1000;

    public static async ValueTask<(IPAddress IPAddress, List<(ushort InternalPort, ushort ExternalPort)> PortMapping)> PerformStunAsync(
        List<IPAddress> stunServerIpAddresses, List<ushort> p2pReservedPorts, AddressFamily addressFamily, CancellationToken cancellationToken)
    {
        Logger.Log($"P2P: Using STUN to detect {addressFamily} address.");

        var stunPortMapping = new List<(ushort InternalPort, ushort ExternalPort)>();
        var matchingStunServerIpAddresses = stunServerIpAddresses.Where(q => q.AddressFamily == addressFamily).ToList();

        if (matchingStunServerIpAddresses.Count is 0)
        {
            Logger.Log($"P2P: No {addressFamily} STUN servers found.");

            return (null, stunPortMapping);
        }

        IPAddress stunPublicAddress = null;
        IPAddress stunServerIpAddress = null;

        foreach (IPAddress matchingStunServerIpAddress in matchingStunServerIpAddresses.TakeWhile(_ => stunPublicAddress is null))
        {
            stunServerIpAddress = matchingStunServerIpAddress;

            foreach (ushort p2pReservedPort in p2pReservedPorts)
            {
                IPEndPoint stunPublicIpEndPoint = await PerformStunAsync(
                    stunServerIpAddress, p2pReservedPort, addressFamily, cancellationToken).ConfigureAwait(false);

                if (stunPublicIpEndPoint is null)
                    break;

                stunPublicAddress = stunPublicIpEndPoint.Address;

                if (p2pReservedPort != stunPublicIpEndPoint.Port)
                    stunPortMapping.Add(new(p2pReservedPort, (ushort)stunPublicIpEndPoint.Port));
            }
        }

        if (stunPublicAddress is not null)
            Logger.Log($"P2P: {addressFamily} STUN detection succeeded using server {stunServerIpAddress}.");
        else
            Logger.Log($"P2P: {addressFamily} STUN detection failed.");

        if (stunPortMapping.Count is not 0)
        {
            Logger.Log($"P2P: {addressFamily} STUN detection detected mapped ports, running STUN keep alive.");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            KeepStunAliveAsync(
                stunServerIpAddress,
                stunPortMapping.Select(q => q.InternalPort).ToList(), cancellationToken).HandleTask();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        return (stunPublicAddress, stunPortMapping);
    }

    private static async ValueTask<IPEndPoint> PerformStunAsync(IPAddress stunServerIpAddress, ushort localPort, AddressFamily addressFamily, CancellationToken cancellationToken)
    {
        const short stunId = 26262;
        const int stunPort1 = 3478;
        const int stunPort2 = 8054;
        const int stunSize = 48;
        int[] stunPorts = { stunPort1, stunPort2 };
        using var socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
        short stunIdNetworkOrder = IPAddress.HostToNetworkOrder(stunId);
        using IMemoryOwner<byte> receiveMemoryOwner = MemoryPool<byte>.Shared.Rent(stunSize);
        Memory<byte> buffer = receiveMemoryOwner.Memory[..stunSize];
#if NETFRAMEWORK
        byte[] bytes = BitConverter.GetBytes(stunIdNetworkOrder);

        for (int i = 0; i < bytes.Length; i++)
        {
            buffer.Span[i] = bytes[i];
        }

        if (!MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> buffer1))
            throw new();
#else
        if (!BitConverter.TryWriteBytes(buffer.Span, stunIdNetworkOrder))
            throw new();
#endif

        IPEndPoint stunServerIpEndPoint = null;
        int addressBytes = stunServerIpAddress.GetAddressBytes().Length;
        const int portBytes = sizeof(ushort);

        socket.Bind(new IPEndPoint(addressFamily is AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, localPort));

        foreach (int stunPort in stunPorts)
        {
            try
            {
                stunServerIpEndPoint = new(stunServerIpAddress, stunPort);

                using var timeoutCancellationTokenSource = new CancellationTokenSource(PingTimeout);
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

#if NETFRAMEWORK
                await socket.SendToAsync(buffer1, SocketFlags.None, stunServerIpEndPoint).WithCancellation(linkedCancellationTokenSource.Token).ConfigureAwait(false);

                SocketReceiveFromResult socketReceiveFromResult = await socket.ReceiveFromAsync(
                    buffer1, SocketFlags.None, stunServerIpEndPoint).WithCancellation(linkedCancellationTokenSource.Token).ConfigureAwait(false);
#else
                await socket.SendToAsync(buffer, stunServerIpEndPoint, linkedCancellationTokenSource.Token).ConfigureAwait(false);

                SocketReceiveFromResult socketReceiveFromResult = await socket.ReceiveFromAsync(
                    buffer, SocketFlags.None, stunServerIpEndPoint, linkedCancellationTokenSource.Token).ConfigureAwait(false);

#endif
                buffer = buffer[..socketReceiveFromResult.ReceivedBytes];

                // de-obfuscate
                for (int i = 0; i < addressBytes + portBytes; i++)
                    buffer.Span[i] ^= 0x20;

#if NETFRAMEWORK
                byte[] publicIpAddressBytes = buffer[..addressBytes].ToArray();
                var publicIpAddress = new IPAddress(publicIpAddressBytes);
                byte[] publicPortBytes = buffer[addressBytes..(addressBytes + portBytes)].ToArray();
                short publicPortNetworkOrder = BitConverter.ToInt16(publicPortBytes, 0);
#else
                ReadOnlyMemory<byte> publicIpAddressBytes = buffer[..addressBytes];
                var publicIpAddress = new IPAddress(publicIpAddressBytes.Span);
                ReadOnlyMemory<byte> publicPortBytes = buffer[addressBytes..(addressBytes + portBytes)];
                short publicPortNetworkOrder = BitConverter.ToInt16(publicPortBytes.Span);
#endif
                short publicPortHostOrder = IPAddress.NetworkToHostOrder(publicPortNetworkOrder);
                ushort publicPort = (ushort)publicPortHostOrder;

                return new(publicIpAddress, publicPort);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                Logger.Log($"P2P: STUN server {stunServerIpEndPoint} unreachable.");
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, $"P2P: STUN server {stunServerIpEndPoint} unreachable.");
            }
        }

        return null;
    }

    private static async Task KeepStunAliveAsync(IPAddress stunServerIpAddress, List<ushort> localPorts, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (ushort localPort in localPorts)
                {
                    await PerformStunAsync(stunServerIpAddress, localPort, stunServerIpAddress.AddressFamily, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log($"P2P: {stunServerIpAddress.AddressFamily} STUN keep alive stopped.");
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, "P2P: STUN keep alive failed.");
        }
    }
}