using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
#if NETFRAMEWORK
using ClientCore.Extensions;
#else
using System.Runtime.Versioning;
#endif
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#else
using System.Collections.ObjectModel;
#endif

namespace DTAClient.Domain.Multiplayer;

internal static class NetworkHelper
{
    private const string PingHost = "cncnet.org";
    private const int PingTimeout = 1000;

#if NET8_0_OR_GREATER
    private static readonly FrozenSet<AddressFamily> SupportedAddressFamilies = new[]
#else
    private static readonly IReadOnlyCollection<AddressFamily> SupportedAddressFamilies = new ReadOnlyCollection<AddressFamily>(new[]
#endif
    {
        AddressFamily.InterNetwork,
        AddressFamily.InterNetworkV6
#if NET8_0_OR_GREATER
    }.ToFrozenSet();
#else
    });
#endif

    public static bool HasIPv6Internet()
        => Socket.OSSupportsIPv6 && GetLocalPublicIpV6Address() is not null;

    public static bool HasIPv4Internet()
        => Socket.OSSupportsIPv4 && GetLocalAddresses().Any(q => q.AddressFamily is AddressFamily.InterNetwork);

    public static IEnumerable<IPAddress> GetLocalAddresses()
        => GetUniCastIpAddresses()
        .Select(q => q.Address);

    public static IEnumerable<IPAddress> GetPublicIpAddresses()
        => GetLocalAddresses()
        .Where(q => !IsPrivateIpAddress(q));

    public static IEnumerable<IPAddress> GetPrivateIpAddresses()
        => GetLocalAddresses()
        .Where(IsPrivateIpAddress);

#if !NETFRAMEWORK
    [SupportedOSPlatform("windows")]
#endif
    public static IEnumerable<UnicastIPAddressInformation> GetWindowsLanUniCastIpAddresses()
        => GetLanUniCastIpAddresses()
        .Where(q => q.SuffixOrigin is not SuffixOrigin.WellKnown);

    public static IEnumerable<UnicastIPAddressInformation> GetLanUniCastIpAddresses()
        => GetIpInterfaces()
        .SelectMany(q => q.UnicastAddresses)
        .Where(q => SupportedAddressFamilies.Contains(q.Address.AddressFamily));

    public static IEnumerable<IPAddress> GetMulticastAddresses()
        => GetIpInterfaces()
        .SelectMany(q => q.MulticastAddresses.Select(r => r.Address))
        .Where(q => SupportedAddressFamilies.Contains(q.AddressFamily));

    public static Uri FormatUri(string scheme, Uri uri, ushort port, string path)
    {
        string[] pathAndQuery = path.Split('?');
        var uriBuilder = new UriBuilder(uri)
        {
            Scheme = scheme,
            Host = uri.IdnHost,
            Port = port,
            Path = pathAndQuery.First(),
            Query = pathAndQuery.Skip(1).SingleOrDefault()
        };

        return uriBuilder.Uri;
    }

    public static Uri FormatUri(IPEndPoint ipEndPoint, string scheme = null, string path = null)
    {
        var uriBuilder = new UriBuilder(scheme ?? Uri.UriSchemeHttps, ipEndPoint.Address.ToString(), ipEndPoint.Port, path);

        return uriBuilder.Uri;
    }

    private static IEnumerable<UnicastIPAddressInformation> GetUniCastIpAddresses()
        => GetIpInterfaces()
        .SelectMany(q => q.UnicastAddresses)
        .Where(q => SupportedAddressFamilies.Contains(q.Address.AddressFamily));

    private static IEnumerable<IPInterfaceProperties> GetIpInterfaces()
        => NetworkInterface.GetAllNetworkInterfaces()
        .Where(q => q.OperationalStatus is OperationalStatus.Up && q.NetworkInterfaceType is not NetworkInterfaceType.Loopback)
        .Select(q => q.GetIPProperties())
        .Where(q => q.GatewayAddresses.Count is not 0);

#if !NETFRAMEWORK
    [SupportedOSPlatform("windows")]
#endif
    private static IEnumerable<(IPAddress IpAddress, PrefixOrigin PrefixOrigin, SuffixOrigin SuffixOrigin)> GetWindowsPublicIpAddresses()
        => GetUniCastIpAddresses()
        .Where(q => !IsPrivateIpAddress(q.Address))
        .Select(q => (q.Address, q.PrefixOrigin, q.SuffixOrigin));

    public static IPAddress GetIpV4BroadcastAddress(UnicastIPAddressInformation unicastIpAddressInformation)
    {
        uint ipAddress = BitConverter.ToUInt32(unicastIpAddressInformation.Address.GetAddressBytes(), 0);
        uint ipMaskV4 = BitConverter.ToUInt32(unicastIpAddressInformation.IPv4Mask.GetAddressBytes(), 0);
        uint broadCastIpAddress = ipAddress | ~ipMaskV4;

        return new(BitConverter.GetBytes(broadCastIpAddress));
    }

    public static IPAddress GetLocalPublicIpV6Address()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetPublicIpAddresses().FirstOrDefault(q => q.AddressFamily is AddressFamily.InterNetworkV6);

        var localIpV6Addresses = GetWindowsPublicIpAddresses()
            .Where(q => q.IpAddress.AddressFamily is AddressFamily.InterNetworkV6).ToList();

        (IPAddress IpAddress, PrefixOrigin PrefixOrigin, SuffixOrigin SuffixOrigin) foundLocalPublicIpV6Address = localIpV6Addresses
            .FirstOrDefault(q => q.PrefixOrigin is PrefixOrigin.RouterAdvertisement && q.SuffixOrigin is SuffixOrigin.LinkLayerAddress);

        if (foundLocalPublicIpV6Address.IpAddress is null)
        {
            foundLocalPublicIpV6Address = localIpV6Addresses.FirstOrDefault(
                q => q.PrefixOrigin is PrefixOrigin.Dhcp && q.SuffixOrigin is SuffixOrigin.OriginDhcp);
        }

        return foundLocalPublicIpV6Address.IpAddress;
    }

    public static async ValueTask<IPAddress> TracePublicIpV4Address(CancellationToken cancellationToken)
    {
        try
        {
#if NETFRAMEWORK
            IPAddress[] ipAddresses = await Dns.GetHostAddressesAsync(PingHost).WithCancellation(cancellationToken).ConfigureAwait(false);
#else
            IPAddress[] ipAddresses = await Dns.GetHostAddressesAsync(PingHost, cancellationToken).ConfigureAwait(false);
#endif
            using var ping = new Ping();

            foreach (IPAddress ipAddress in ipAddresses.Where(q => q.AddressFamily is AddressFamily.InterNetwork))
            {
                PingReply pingReply = await ping.SendPingAsync(ipAddress, PingTimeout).ConfigureAwait(false);

                if (pingReply.Status is not IPStatus.Success)
                    continue;

                IPAddress pingIpAddress = null;
                int ttl = 1;

                while (!ipAddress.Equals(pingIpAddress))
                {
                    pingReply = await ping.SendPingAsync(ipAddress, PingTimeout, Array.Empty<byte>(), new(ttl++, false)).ConfigureAwait(false);
                    pingIpAddress = pingReply.Address;

                    if (ipAddress.Equals(pingIpAddress))
                        break;

                    if (!IsPrivateIpAddress(pingReply.Address))
                        return pingReply.Address;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ProgramConstants.LogException(ex, "IP trace detection failed.");
        }

        return null;
    }

    public static async ValueTask<long?> PingAsync(IPAddress ipAddress)
    {
        if ((ipAddress.AddressFamily is AddressFamily.InterNetworkV6 && !HasIPv6Internet())
            || (ipAddress.AddressFamily is AddressFamily.InterNetwork && !HasIPv4Internet()))
        {
            return null;
        }

        using var ping = new Ping();

        try
        {
            PingReply pingResult = await ping.SendPingAsync(ipAddress, PingTimeout).ConfigureAwait(false);

            if (pingResult.Status is IPStatus.Success)
                return pingResult.RoundtripTime;
        }
        catch (PingException ex)
        {
            ProgramConstants.LogException(ex, "Ping failed.");
        }

        return null;
    }

    /// <summary>
    /// Returns the specified amount of free UDP port numbers.
    /// </summary>
    /// <param name="excludedPorts">List of UDP port numbers which are additionally excluded.</param>
    /// <param name="numberOfPorts">The number of free ports to return.</param>
    /// <returns>A free UDP port number on the current system.</returns>
    public static IEnumerable<ushort> GetFreeUdpPorts(IEnumerable<ushort> excludedPorts, ushort numberOfPorts)
    {
        IPEndPoint[] endPoints = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
        var activeV4AndV6Ports = endPoints.Select(q => (ushort)q.Port).ToArray().Concat(excludedPorts).Distinct().ToList();
        ushort foundPortCount = 0;

        while (foundPortCount != numberOfPorts)
        {
            using var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            ushort foundPort = (ushort)((IPEndPoint)socket.LocalEndPoint).Port;

            if (!activeV4AndV6Ports.Contains(foundPort))
            {
                activeV4AndV6Ports.Add(foundPort);

                foundPortCount++;

                yield return foundPort;
            }
        }
    }

    public static bool IsPrivateIpAddress(IPAddress ipAddress)
        => ipAddress.AddressFamily switch
        {
            AddressFamily.InterNetworkV6 => ipAddress.IsIPv6SiteLocal
#if !NETFRAMEWORK
                || ipAddress.IsIPv6UniqueLocal
#endif
                || ipAddress.IsIPv6LinkLocal,
            AddressFamily.InterNetwork => IsInRange("10.0.0.0", "10.255.255.255", ipAddress)
                || IsInRange("172.16.0.0", "172.31.255.255", ipAddress)
                || IsInRange("192.168.0.0", "192.168.255.255", ipAddress)
                || IsInRange("169.254.0.0", "169.254.255.255", ipAddress)
                || IsInRange("127.0.0.0", "127.255.255.255", ipAddress)
                || IsInRange("0.0.0.0", "0.255.255.255", ipAddress),
            _ => throw new ArgumentOutOfRangeException(nameof(ipAddress.AddressFamily), ipAddress.AddressFamily, null),
        };

    private static bool IsInRange(string startIpAddress, string endIpAddress, IPAddress address)
    {
        uint ipStart = BitConverter.ToUInt32(IPAddress.Parse(startIpAddress).GetAddressBytes().Reverse().ToArray(), 0);
        uint ipEnd = BitConverter.ToUInt32(IPAddress.Parse(endIpAddress).GetAddressBytes().Reverse().ToArray(), 0);
        uint ip = BitConverter.ToUInt32(address.GetAddressBytes().Reverse().ToArray(), 0);

        return ip >= ipStart && ip <= ipEnd;
    }
}