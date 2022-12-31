﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

internal static class UPnPHandler
{
    private const int ReceiveTimeout = 2000;
    private const int SendCount = 3;

    private static readonly HttpClient HttpClient = new(
        new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            SslOptions = new()
            {
                CertificateChainPolicy = new()
                {
                    DisableCertificateDownloads = true
                }
            }
        },
        true)
    {
        Timeout = TimeSpan.FromMilliseconds(ReceiveTimeout),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
    };

    private static IReadOnlyDictionary<AddressType, IPAddress> SsdpMultiCastAddresses
        => new Dictionary<AddressType, IPAddress>
        {
            [AddressType.IpV4SiteLocal] = IPAddress.Parse("239.255.255.250"),
            [AddressType.IpV6LinkLocal] = IPAddress.Parse("[FF02::C]"),
            [AddressType.IpV6SiteLocal] = IPAddress.Parse("[FF05::C]")
        }.AsReadOnly();

    public static async ValueTask<(
        InternetGatewayDevice InternetGatewayDevice,
        List<(ushort InternalPort, ushort ExternalPort)> IpV6P2PPorts,
        List<(ushort InternalPort, ushort ExternalPort)> IpV4P2PPorts,
        List<ushort> P2PIpV6PortIds,
        IPAddress IpV6Address,
        IPAddress IpV4Address)> SetupPortsAsync(
        InternetGatewayDevice internetGatewayDevice,
        List<ushort> p2pReservedPorts,
        List<IPAddress> stunServerIpAddresses,
        CancellationToken cancellationToken)
    {
        Logger.Log("P2P: Starting Setup.");

        internetGatewayDevice ??= await GetInternetGatewayDeviceAsync(cancellationToken).ConfigureAwait(false);

        Task<(IPAddress IpAddress, List<(ushort InternalPort, ushort ExternalPort)> Ports)> ipV4Task = SetupIpV4PortsAsync(internetGatewayDevice, p2pReservedPorts, stunServerIpAddresses, cancellationToken);
        Task<(IPAddress IpAddress, List<(ushort InternalPort, ushort ExternalPort)> Ports, List<ushort> PortIds)> ipV6Task = SetupIpV6PortsAsync(internetGatewayDevice, p2pReservedPorts, stunServerIpAddresses, cancellationToken);

        await ClientCore.Extensions.TaskExtensions.WhenAllSafe(new Task[] { ipV4Task, ipV6Task }).ConfigureAwait(false);

        return (internetGatewayDevice, ipV6Task.Result.Ports, ipV4Task.Result.Ports, ipV6Task.Result.PortIds, ipV6Task.Result.IpAddress, ipV4Task.Result.IpAddress);
    }

    private static async Task<InternetGatewayDevice> GetInternetGatewayDeviceAsync(CancellationToken cancellationToken)
    {
        var internetGatewayDevices = (await GetInternetGatewayDevicesAsync(cancellationToken).ConfigureAwait(false)).ToList();
        InternetGatewayDevice internetGatewayDevice = GetInternetGatewayDevice(internetGatewayDevices, 2);

        internetGatewayDevice ??= GetInternetGatewayDevice(internetGatewayDevices, 1);

        if (internetGatewayDevice is not null)
            Logger.Log($"P2P: Found NAT device {internetGatewayDevice.UPnPDescription.Device.DeviceType} - {internetGatewayDevice.Server} ({internetGatewayDevice.UPnPDescription.Device.FriendlyName}).");

        return internetGatewayDevice;
    }

    private static async Task<(IPAddress IpAddress, List<(ushort InternalPort, ushort ExternalPort)> Ports, List<ushort> PortIds)> SetupIpV6PortsAsync(
        InternetGatewayDevice internetGatewayDevice, List<ushort> p2pReservedPorts, List<IPAddress> stunServerIpAddresses, CancellationToken cancellationToken)
    {
        (IPAddress stunPublicIpV6Address, List<(ushort InternalPort, ushort ExternalPort)> ipV6StunPortMapping) = await NetworkHelper.PerformStunAsync(
            stunServerIpAddresses, p2pReservedPorts, AddressFamily.InterNetworkV6, cancellationToken).ConfigureAwait(false);
        IPAddress localPublicIpV6Address = NetworkHelper.GetLocalPublicIpV6Address();
        var ipV6P2PPorts = new List<(ushort InternalPort, ushort ExternalPort)>();
        var ipV6P2PPortIds = new List<ushort>();
        IPAddress publicIpV6Address = null;

        if (stunPublicIpV6Address is not null || localPublicIpV6Address is not null)
        {
            Logger.Log("P2P: Public IPV6 detected.");

            if (internetGatewayDevice is not null)
            {
                try
                {
                    (bool? firewallEnabled, bool? inboundPinholeAllowed) = await internetGatewayDevice.GetIpV6FirewallStatusAsync(
                        cancellationToken).ConfigureAwait(false);

                    if (firewallEnabled is not false && inboundPinholeAllowed is not false)
                    {
                        Logger.Log("P2P: Configuring IPV6 firewall.");

                        ipV6P2PPortIds = (await ClientCore.Extensions.TaskExtensions.WhenAllSafe(p2pReservedPorts.Select(
                            q => internetGatewayDevice.OpenIpV6PortAsync(localPublicIpV6Address, q, cancellationToken))).ConfigureAwait(false)).ToList();
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    ProgramConstants.LogException(ex, $"P2P: Could not open P2P IPV6 router ports for {localPublicIpV6Address}.");
#else
                    ProgramConstants.LogException(ex, $"P2P: Could not open P2P IPV6 router ports.");
#endif
                }
            }

            if (stunPublicIpV6Address is not null && localPublicIpV6Address is not null && !stunPublicIpV6Address.Equals(localPublicIpV6Address))
            {
                publicIpV6Address = stunPublicIpV6Address;
                ipV6P2PPorts = ipV6StunPortMapping.Any() ? ipV6StunPortMapping : p2pReservedPorts.Select(q => (q, q)).ToList();
            }
            else
            {
                publicIpV6Address = stunPublicIpV6Address ?? localPublicIpV6Address;
                ipV6P2PPorts = p2pReservedPorts.Select(q => (q, q)).ToList();
            }
        }

        return (publicIpV6Address, ipV6P2PPorts, ipV6P2PPortIds);
    }

    private static async Task<(IPAddress IpAddress, List<(ushort InternalPort, ushort ExternalPort)> Ports)> SetupIpV4PortsAsync(
        InternetGatewayDevice internetGatewayDevice, List<ushort> p2pReservedPorts, List<IPAddress> stunServerIpAddresses, CancellationToken cancellationToken)
    {
        bool? routerNatEnabled = null;
        IPAddress routerPublicIpV4Address = null;

        if (internetGatewayDevice is not null)
        {
            Task<bool?> natRsipStatusTask = internetGatewayDevice.GetNatRsipStatusAsync(cancellationToken);
            Task<IPAddress> externalIpv4AddressTask = internetGatewayDevice.GetExternalIpV4AddressAsync(cancellationToken);

            await ClientCore.Extensions.TaskExtensions.WhenAllSafe(new Task[] { natRsipStatusTask, externalIpv4AddressTask }).ConfigureAwait(false);

            routerNatEnabled = natRsipStatusTask.Result;
            routerPublicIpV4Address = externalIpv4AddressTask.Result;
        }

        (IPAddress stunPublicIpV4Address, List<(ushort InternalPort, ushort ExternalPort)> ipV4StunPortMapping) = await NetworkHelper.PerformStunAsync(
            stunServerIpAddresses, p2pReservedPorts, AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
        IPAddress tracePublicIpV4Address = null;

        if (routerPublicIpV4Address is null && stunPublicIpV4Address is null)
        {
            Logger.Log("P2P: Using IPV4 trace detection.");

            tracePublicIpV4Address = await NetworkHelper.TracePublicIpV4Address(cancellationToken).ConfigureAwait(false);
        }

        IPAddress localPublicIpV4Address = null;

        if (routerPublicIpV4Address is null && stunPublicIpV4Address is null && tracePublicIpV4Address is null)
        {
            Logger.Log("P2P: Using IPV4 local public address.");

            var localPublicIpAddresses = NetworkHelper.GetPublicIpAddresses().ToList();

            localPublicIpV4Address = localPublicIpAddresses.FirstOrDefault(q => q.AddressFamily is AddressFamily.InterNetwork);
        }

        IPAddress publicIpV4Address = stunPublicIpV4Address ?? routerPublicIpV4Address ?? tracePublicIpV4Address ?? localPublicIpV4Address;
        var ipV4P2PPorts = new List<(ushort InternalPort, ushort ExternalPort)>();

        if (publicIpV4Address is not null)
        {
            Logger.Log("P2P: Public IPV4 detected.");

            var privateIpV4Addresses = NetworkHelper.GetPrivateIpAddresses().Where(q => q.AddressFamily is AddressFamily.InterNetwork).ToList();
            IPAddress privateIpV4Address = privateIpV4Addresses.FirstOrDefault();

            if (internetGatewayDevice is not null && privateIpV4Address is not null && routerNatEnabled is not false)
            {
                Logger.Log("P2P: Using IPV4 port mapping.");

                try
                {
                    ipV4P2PPorts = (await ClientCore.Extensions.TaskExtensions.WhenAllSafe(p2pReservedPorts.Select(
                        q => internetGatewayDevice.OpenIpV4PortAsync(privateIpV4Address, q, cancellationToken))).ConfigureAwait(false)).Select(q => (q, q)).ToList();
                    p2pReservedPorts = ipV4P2PPorts.Select(q => q.InternalPort).ToList();
                }
                catch (Exception ex)
                {
#if DEBUG
                    ProgramConstants.LogException(ex, $"P2P: Could not open P2P IPV4 router ports for {privateIpV4Address} -> {publicIpV4Address}.");
#else
                    ProgramConstants.LogException(ex, $"P2P: Could not open P2P IPV4 router ports.");
#endif
                    ipV4P2PPorts = ipV4StunPortMapping.Any() ? ipV4StunPortMapping : p2pReservedPorts.Select(q => (q, q)).ToList();
                }
            }
            else
            {
                ipV4P2PPorts = ipV4StunPortMapping.Any() ? ipV4StunPortMapping : p2pReservedPorts.Select(q => (q, q)).ToList();
            }
        }

        return (publicIpV4Address, ipV4P2PPorts);
    }

    private static async ValueTask<IEnumerable<InternetGatewayDevice>> GetInternetGatewayDevicesAsync(CancellationToken cancellationToken)
    {
        IEnumerable<string> rawDeviceResponses = await GetRawDeviceResponses(cancellationToken).ConfigureAwait(false);
        IEnumerable<Dictionary<string, string>> formattedDeviceResponses = GetFormattedDeviceResponses(rawDeviceResponses);
        IEnumerable<IGrouping<string, InternetGatewayDeviceResponse>> groupedInternetGatewayDeviceResponses =
            GetGroupedInternetGatewayDeviceResponses(formattedDeviceResponses);

        return await ClientCore.Extensions.TaskExtensions.WhenAllSafe(
            groupedInternetGatewayDeviceResponses.Select(q => GetInternetGatewayDeviceAsync(q, cancellationToken))).ConfigureAwait(false);
    }

    private static InternetGatewayDevice GetInternetGatewayDevice(List<InternetGatewayDevice> internetGatewayDevices, ushort uPnPVersion)
        => internetGatewayDevices.SingleOrDefault(q => $"{UPnPConstants.UPnPInternetGatewayDevice}:{uPnPVersion}".Equals(q.UPnPDescription.Device.DeviceType, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<IGrouping<string, InternetGatewayDeviceResponse>> GetGroupedInternetGatewayDeviceResponses(
        IEnumerable<Dictionary<string, string>> formattedDeviceResponses)
    {
        return formattedDeviceResponses
            .Select(q => new InternetGatewayDeviceResponse(new(q["LOCATION"]), q["SERVER"], q["CACHE-CONTROL"], q["EXT"], q["ST"], q["USN"]))
            .GroupBy(q => q.Usn);
    }

    private static Uri GetPreferredLocation(IReadOnlyCollection<Uri> locations)
    {
        return locations.FirstOrDefault(q => q.HostNameType is UriHostNameType.IPv6) ?? locations.First(q => q.HostNameType is UriHostNameType.IPv4);
    }

    private static IEnumerable<Dictionary<string, string>> GetFormattedDeviceResponses(IEnumerable<string> responses)
    {
        return responses.Select(q => q.Split(Environment.NewLine)).Select(q => q.Where(r => r.Contains(':', StringComparison.OrdinalIgnoreCase)).ToDictionary(
            s => s[..s.IndexOf(':', StringComparison.OrdinalIgnoreCase)],
            s =>
            {
                string value = s[s.IndexOf(':', StringComparison.OrdinalIgnoreCase)..];

                if (value.EndsWith(":", StringComparison.OrdinalIgnoreCase))
                    return value.Replace(":", null, StringComparison.OrdinalIgnoreCase);

                return value.Replace(": ", null, StringComparison.OrdinalIgnoreCase);
            },
            StringComparer.OrdinalIgnoreCase));
    }

    private static async Task<IEnumerable<string>> SearchDevicesAsync(IPAddress localAddress, CancellationToken cancellationToken)
    {
        var responses = new List<string>();
        AddressType addressType = GetAddressType(localAddress);

        if (addressType is AddressType.Unknown)
            return responses;

        var socket = new Socket(localAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        try
        {
            socket.ExclusiveAddressUse = true;

            socket.Bind(new IPEndPoint(localAddress, 0));

            var multiCastIpEndPoint = new IPEndPoint(SsdpMultiCastAddresses[addressType], UPnPConstants.UPnPMultiCastPort);
            string request = FormattableString.Invariant($"M-SEARCH * HTTP/1.1\r\nHOST: {multiCastIpEndPoint}\r\nST: {UPnPConstants.UPnPRootDevice}\r\nMAN: \"ssdp:discover\"\r\nMX: 3\r\n\r\n");
            const int charSize = sizeof(char);
            int bufferSize = request.Length * charSize;
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
            Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
            int bytes = Encoding.UTF8.GetBytes(request.AsSpan(), buffer.Span);

            buffer = buffer[..bytes];

            for (int i = 0; i < SendCount; i++)
            {
                await socket.SendToAsync(buffer, SocketFlags.None, multiCastIpEndPoint, cancellationToken).ConfigureAwait(false);
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            await ReceiveAsync(socket, responses, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            socket.Close();
        }

        return responses;
    }

    private static AddressType GetAddressType(IPAddress localAddress)
    {
        if (localAddress.AddressFamily == AddressFamily.InterNetwork)
            return AddressType.IpV4SiteLocal;

        if (localAddress.IsIPv6LinkLocal)
            return AddressType.IpV6LinkLocal;

        if (localAddress.IsIPv6SiteLocal)
            return AddressType.IpV6SiteLocal;

        return AddressType.Unknown;
    }

    private static async ValueTask ReceiveAsync(Socket socket, ICollection<string> responses, CancellationToken cancellationToken)
    {
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);
        using var timeoutCancellationTokenSource = new CancellationTokenSource(ReceiveTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

        while (!linkedCancellationTokenSource.IsCancellationRequested)
        {
            Memory<byte> buffer = memoryOwner.Memory[..4096];

            try
            {
                int bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None, linkedCancellationTokenSource.Token).ConfigureAwait(false);

                responses.Add(Encoding.UTF8.GetString(buffer.Span[..bytesReceived]));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    private static async ValueTask<UPnPDescription> GetUPnPDescription(Uri uri, CancellationToken cancellationToken)
    {
        Stream uPnPDescription = await HttpClient.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);

        await using (uPnPDescription.ConfigureAwait(false))
        {
            using var xmlTextReader = new XmlTextReader(uPnPDescription);

            return (UPnPDescription)new DataContractSerializer(typeof(UPnPDescription)).ReadObject(xmlTextReader);
        }
    }

    private static async ValueTask<IEnumerable<string>> GetRawDeviceResponses(CancellationToken cancellationToken)
    {
        IEnumerable<IPAddress> localAddresses = NetworkHelper.GetLocalAddresses();
        IEnumerable<string>[] localAddressesDeviceResponses = await ClientCore.Extensions.TaskExtensions.WhenAllSafe(
            localAddresses.Select(q => SearchDevicesAsync(q, cancellationToken))).ConfigureAwait(false);

        return localAddressesDeviceResponses.Where(q => q.Any()).SelectMany(q => q).Distinct();
    }

    private static async Task<InternetGatewayDevice> GetInternetGatewayDeviceAsync(
        IGrouping<string, InternetGatewayDeviceResponse> internetGatewayDeviceResponses, CancellationToken cancellationToken)
    {
        Uri[] locations = internetGatewayDeviceResponses.Select(r => r.Location).ToArray();
        Uri location = GetPreferredLocation(locations);
        UPnPDescription uPnPDescription = default;

        try
        {
            uPnPDescription = await GetUPnPDescription(location, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (location.HostNameType is UriHostNameType.IPv6 && locations.Any(q => q.HostNameType is UriHostNameType.IPv4))
            {
                try
                {
                    location = locations.First(q => q.HostNameType is UriHostNameType.IPv4);

                    uPnPDescription = await GetUPnPDescription(location, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                }
            }
        }

        return new(
            internetGatewayDeviceResponses.Select(r => r.Location).Distinct(),
            internetGatewayDeviceResponses.Select(r => r.Server).Distinct().Single(),
            internetGatewayDeviceResponses.Select(r => r.CacheControl).Distinct().Single(),
            internetGatewayDeviceResponses.Select(r => r.Ext).Distinct().Single(),
            internetGatewayDeviceResponses.Select(r => r.SearchTarget).Distinct().Single(),
            internetGatewayDeviceResponses.Key,
            uPnPDescription,
            location);
    }
}