﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;
using System.ServiceModel.Channels;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

internal sealed record InternetGatewayDevice(
    IEnumerable<Uri> Locations,
    string Server,
    string CacheControl,
    string Ext,
    string SearchTarget,
    string UniqueServiceName,
    UPnPDescription UPnPDescription,
    Uri PreferredLocation)
{
    private const int ReceiveTimeout = 2000;
    private const uint IpLeaseTimeInSeconds = 4 * 60 * 60;
    private const ushort IanaUdpProtocolNumber = 17;
    private const string PortMappingDescription = "CnCNet";

    private static readonly HttpClient HttpClient = new(
        new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            ConnectCallback = async (context, token) =>
            {
                Socket socket = null;

                try
                {
                    socket = new(SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true
                    };

                    if (IPAddress.Parse(context.DnsEndPoint.Host).AddressFamily is AddressFamily.InterNetworkV6)
                        socket.Bind(new IPEndPoint(NetworkHelper.GetLocalPublicIpV6Address(), 0));

                    await socket.ConnectAsync(context.DnsEndPoint, token).ConfigureAwait(false);

                    return new NetworkStream(socket, true);
                }
                catch
                {
                    socket?.Dispose();

                    throw;
                }
            },
            SslOptions = new()
            {
                CertificateChainPolicy = new()
                {
                    DisableCertificateDownloads = true
                }
            }
        }, true)
    {
        Timeout = TimeSpan.FromMilliseconds(ReceiveTimeout),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
    };

    public async Task<ushort> OpenIpV4PortAsync(IPAddress ipAddress, ushort port, CancellationToken cancellationToken)
    {
        Logger.Log($"P2P: Opening IPV4 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        int uPnPVersion = GetDeviceUPnPVersion();

        switch (uPnPVersion)
        {
            case 2:
                var addAnyPortMappingRequest = new AddAnyPortMappingRequest(string.Empty, port, "UDP", port, ipAddress.ToString(), 1, PortMappingDescription, IpLeaseTimeInSeconds);
                AddAnyPortMappingResponse addAnyPortMappingResponse = await DoSoapActionAsync<AddAnyPortMappingRequest, AddAnyPortMappingResponse>(
                    addAnyPortMappingRequest, $"{UPnPConstants.WanIpConnection}:{uPnPVersion}", "AddAnyPortMapping", AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);

                port = addAnyPortMappingResponse.ReservedPort;

                break;
            case 1:
                var addPortMappingRequest = new AddPortMappingRequest(string.Empty, port, "UDP", port, ipAddress.ToString(), 1, PortMappingDescription, IpLeaseTimeInSeconds);

                await DoSoapActionAsync<AddPortMappingRequest, AddPortMappingResponse>(
                     addPortMappingRequest, $"{UPnPConstants.WanIpConnection}:{uPnPVersion}", "AddPortMapping", AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);

                break;
            default:
                throw new ArgumentException($"P2P: UPnP version {uPnPVersion} is not supported.");
        }

        Logger.Log($"P2P: Opened IPV4 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        return port;
    }

    public async Task CloseIpV4PortAsync(ushort port, CancellationToken cancellationToken)
    {
        try
        {
            Logger.Log($"P2P: Deleting IPV4 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");

            int uPnPVersion = GetDeviceUPnPVersion();

            switch (uPnPVersion)
            {
                case 2:
                    var deletePortMappingRequestV2 = new DeletePortMappingRequestV2(string.Empty, port, "UDP");

                    await DoSoapActionAsync<DeletePortMappingRequestV2, DeletePortMappingResponseV2>(
                        deletePortMappingRequestV2, $"{UPnPConstants.WanIpConnection}:{uPnPVersion}", "DeletePortMapping", AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);

                    break;
                case 1:
                    var deletePortMappingRequestV1 = new DeletePortMappingRequestV1(string.Empty, port, "UDP");

                    await DoSoapActionAsync<DeletePortMappingRequestV1, DeletePortMappingResponseV1>(
                        deletePortMappingRequestV1, $"{UPnPConstants.WanIpConnection}:{uPnPVersion}", "DeletePortMapping", AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);

                    break;
                default:
                    throw new ArgumentException($"P2P: UPnP version {uPnPVersion} is not supported.");
            }

            Logger.Log($"P2P: Deleted IPV4 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, $"P2P: Could not close UPnP IPV4 port {port}.");
        }
    }

    public async Task<IPAddress> GetExternalIpV4AddressAsync(CancellationToken cancellationToken)
    {
        Logger.Log($"P2P: Requesting external IP address from UPnP device {UPnPDescription.Device.FriendlyName}.");

        int uPnPVersion = GetDeviceUPnPVersion();
        IPAddress ipAddress = null;

        try
        {
            switch (uPnPVersion)
            {
                case 2:
                    GetExternalIPAddressResponseV2 getExternalIpAddressResponseV2 = await DoSoapActionAsync<GetExternalIPAddressRequestV2, GetExternalIPAddressResponseV2>(
                        default, $"{UPnPConstants.WanIpConnection}:{uPnPVersion}", "GetExternalIPAddress", AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);

                    ipAddress = string.IsNullOrWhiteSpace(getExternalIpAddressResponseV2.ExternalIPAddress) ? null : IPAddress.Parse(getExternalIpAddressResponseV2.ExternalIPAddress);

                    break;
                case 1:
                    GetExternalIPAddressResponseV1 getExternalIpAddressResponseV1 = await DoSoapActionAsync<GetExternalIPAddressRequestV1, GetExternalIPAddressResponseV1>(
                        default, $"{UPnPConstants.WanIpConnection}:{uPnPVersion}", "GetExternalIPAddress", AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);

                    ipAddress = string.IsNullOrWhiteSpace(getExternalIpAddressResponseV1.ExternalIPAddress) ? null : IPAddress.Parse(getExternalIpAddressResponseV1.ExternalIPAddress);
                    break;
                default:
                    throw new ArgumentException($"P2P: UPnP version {uPnPVersion} is not supported.");
            }

            Logger.Log($"P2P: Received external IP address {ipAddress} from UPnP device {UPnPDescription.Device.FriendlyName}.");
        }
        catch
        {
        }

        return ipAddress;
    }

    public async Task<bool?> GetNatRsipStatusAsync(CancellationToken cancellationToken)
    {
        Logger.Log($"P2P: Checking NAT status on UPnP device {UPnPDescription.Device.FriendlyName}.");

        int uPnPVersion = GetDeviceUPnPVersion();
        bool? natEnabled = null;

        try
        {
            switch (uPnPVersion)
            {
                case 2:
                    GetNatRsipStatusResponseV2 getNatRsipStatusResponseV2 = await DoSoapActionAsync<GetNatRsipStatusRequestV2, GetNatRsipStatusResponseV2>(
                        default, $"{UPnPConstants.WanIpConnection}:{uPnPVersion}", "GetNatRsipStatus", AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);

                    natEnabled = getNatRsipStatusResponseV2.NatEnabled;

                    break;
                case 1:
                    GetNatRsipStatusResponseV1 getNatRsipStatusResponseV1 = await DoSoapActionAsync<GetNatRsipStatusRequestV1, GetNatRsipStatusResponseV1>(
                        default, $"{UPnPConstants.WanIpConnection}:{uPnPVersion}", "GetNatRsipStatus", AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);

                    natEnabled = getNatRsipStatusResponseV1.NatEnabled;
                    break;
                default:
                    throw new ArgumentException($"P2P: UPnP version {uPnPVersion} is not supported.");
            }

            Logger.Log($"P2P: Received NAT status {natEnabled} on UPnP device {UPnPDescription.Device.FriendlyName}.");
        }
        catch
        {
        }

        return natEnabled;
    }

    public async ValueTask<(bool? FirewallEnabled, bool? InboundPinholeAllowed)> GetIpV6FirewallStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.Log($"P2P: Checking IPV6 firewall status on UPnP device {UPnPDescription.Device.FriendlyName}.");

            GetFirewallStatusResponse response = await DoSoapActionAsync<GetFirewallStatusRequest, GetFirewallStatusResponse>(
                default, $"{UPnPConstants.WanIpv6FirewallControl}:1", "GetFirewallStatus", AddressFamily.InterNetworkV6, cancellationToken).ConfigureAwait(false);

            Logger.Log($"P2P: Received IPV6 firewall status {response.FirewallEnabled} and port mapping allowed {response.InboundPinholeAllowed} on UPnP device {UPnPDescription.Device.FriendlyName}.");

            return (response.FirewallEnabled, response.InboundPinholeAllowed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return (null, null);
        }
    }

    public async Task<ushort> OpenIpV6PortAsync(IPAddress ipAddress, ushort port, CancellationToken cancellationToken)
    {
        Logger.Log($"P2P: Opening IPV6 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        var request = new AddPinholeRequest(string.Empty, port, ipAddress.ToString(), port, IanaUdpProtocolNumber, IpLeaseTimeInSeconds);
        AddPinholeResponse response = await DoSoapActionAsync<AddPinholeRequest, AddPinholeResponse>(
            request, $"{UPnPConstants.WanIpv6FirewallControl}:1", "AddPinhole", AddressFamily.InterNetworkV6, cancellationToken).ConfigureAwait(false);

        Logger.Log($"P2P: Opened IPV6 UDP port {port} with ID {response.UniqueId} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        return response.UniqueId;
    }

    public async Task CloseIpV6PortAsync(ushort uniqueId, CancellationToken cancellationToken)
    {
        try
        {
            Logger.Log($"P2P: Deleting IPV6 UDP port with ID {uniqueId} on UPnP device {UPnPDescription.Device.FriendlyName}.");
            await DoSoapActionAsync<DeletePinholeRequest, DeletePinholeResponse>(
                new(uniqueId), $"{UPnPConstants.WanIpv6FirewallControl}:1", "DeletePinhole", AddressFamily.InterNetworkV6, cancellationToken).ConfigureAwait(false);
            Logger.Log($"P2P: Deleted IPV6 UDP port with ID {uniqueId} on UPnP device {UPnPDescription.Device.FriendlyName}.");
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, $"P2P: Could not close UPnP IPV6 port with id {uniqueId}.");
        }
    }

    private async ValueTask<TResponse> DoSoapActionAsync<TRequest, TResponse>(
        TRequest request, string wanConnectionDeviceService, string action, AddressFamily addressFamily, CancellationToken cancellationToken)
    {
        try
        {
            (ServiceListItem service, Uri serviceUri, string serviceType) = GetSoapActionParameters(wanConnectionDeviceService, addressFamily);
            string soapAction = $"\"{service.ServiceType}#{action}\"";

            return await ExecuteSoapAction<TRequest, TResponse>(serviceUri, soapAction, serviceType, request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ProgramConstants.LogException(ex, $"P2P: {action} error/not supported on UPnP device {UPnPDescription.Device.FriendlyName} using {addressFamily}.");

            throw;
        }
    }

    private static async ValueTask<TResponse> ExecuteSoapAction<TRequest, TResponse>(
        Uri serviceUri, string soapAction, string defaultNamespace, TRequest request, CancellationToken cancellationToken)
    {
        HttpClient.DefaultRequestHeaders.Remove("SOAPAction");
        HttpClient.DefaultRequestHeaders.Add("SOAPAction", soapAction);

        var xmlSerializerFormatAttribute = new XmlSerializerFormatAttribute
        {
            Style = OperationFormatStyle.Rpc,
            Use = OperationFormatUse.Encoded
        };
        var requestTypedMessageConverter = TypedMessageConverter.Create(typeof(TRequest), soapAction, defaultNamespace, xmlSerializerFormatAttribute);
        using var requestMessage = requestTypedMessageConverter.ToMessage(request);
        var requestStream = new MemoryStream();
        HttpResponseMessage httpResponseMessage;

        await using (requestStream)
        {
            var writer = XmlWriter.Create(
                requestStream,
                new()
                {
                    OmitXmlDeclaration = true,
                    Async = true,
                    Encoding = new UTF8Encoding()
                });

            await using (writer.ConfigureAwait(false))
            {
                requestMessage.WriteMessage(writer);
                await writer.FlushAsync().ConfigureAwait(false);
            }

            requestStream.Position = 0L;

            using var content = new StreamContent(requestStream);

            content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

            httpResponseMessage = await HttpClient.PostAsync(serviceUri, content, cancellationToken).ConfigureAwait(false);
        }

        using (httpResponseMessage)
        {
            Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            await using (stream.ConfigureAwait(false))
            {
                try
                {
                    httpResponseMessage.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException ex)
                {
                    using var reader = new StreamReader(stream);
                    string error = await reader.ReadToEndAsync(CancellationToken.None).ConfigureAwait(false);

                    ProgramConstants.LogException(ex, $"P2P: UPnP error {ex.StatusCode}:{error}.");

                    throw;
                }

                using var envelopeReader = XmlDictionaryReader.CreateTextReader(stream, new());
                using var responseMessage = Message.CreateMessage(envelopeReader, int.MaxValue, MessageVersion.Soap11WSAddressingAugust2004);
                var responseTypedMessageConverter = TypedMessageConverter.Create(typeof(TResponse), null, defaultNamespace, xmlSerializerFormatAttribute);

                return (TResponse)responseTypedMessageConverter.FromMessage(responseMessage);
            }
        }
    }

    private (ServiceListItem WanIpConnectionService, Uri ServiceUri, string ServiceType) GetSoapActionParameters(string wanConnectionDeviceService, AddressFamily addressFamily)
    {
        Uri location = addressFamily switch
        {
            AddressFamily.InterNetwork when Locations.Any(q => q.HostNameType is UriHostNameType.IPv4) =>
                Locations.FirstOrDefault(q => q.HostNameType is UriHostNameType.IPv4),
            AddressFamily.InterNetworkV6 when Locations.Any(q => q.HostNameType is UriHostNameType.IPv6) =>
                Locations.FirstOrDefault(q => q.HostNameType is UriHostNameType.IPv6),
            _ => PreferredLocation
        };
        int uPnPVersion = GetDeviceUPnPVersion();
        Device wanDevice = UPnPDescription.Device.DeviceList.Single(q => q.DeviceType.Equals($"{UPnPConstants.UPnPWanDevice}:{uPnPVersion}", StringComparison.OrdinalIgnoreCase));
        Device wanConnectionDevice = wanDevice.DeviceList.Single(q => q.DeviceType.Equals($"{UPnPConstants.UPnPWanConnectionDevice}:{uPnPVersion}", StringComparison.OrdinalIgnoreCase));
        string serviceType = $"{UPnPConstants.UPnPServiceNamespace}:{wanConnectionDeviceService}";
        ServiceListItem wanIpConnectionService = wanConnectionDevice.ServiceList.Single(q => q.ServiceType.Equals(serviceType, StringComparison.OrdinalIgnoreCase));
        var serviceUri = new Uri(FormattableString.Invariant($"{location.Scheme}://{location.Authority}{wanIpConnectionService.ControlUrl}"));

        return new(wanIpConnectionService, serviceUri, serviceType);
    }

    private int GetDeviceUPnPVersion()
    {
        return $"{UPnPConstants.UPnPInternetGatewayDevice}:2".Equals(UPnPDescription.Device.DeviceType, StringComparison.OrdinalIgnoreCase) ? 2
            : $"{UPnPConstants.UPnPInternetGatewayDevice}:1".Equals(UPnPDescription.Device.DeviceType, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }
}