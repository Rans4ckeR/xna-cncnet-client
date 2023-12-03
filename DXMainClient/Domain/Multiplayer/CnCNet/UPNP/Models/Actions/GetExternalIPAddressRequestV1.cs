using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = UPnPConstants.GetExternalIpAddress, WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpConnection}:1")]
internal readonly record struct GetExternalIPAddressRequestV1;