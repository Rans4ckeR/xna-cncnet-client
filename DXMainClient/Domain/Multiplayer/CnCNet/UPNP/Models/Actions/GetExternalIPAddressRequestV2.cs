﻿using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = UPnPConstants.GetExternalIpAddress, WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpConnection}:2")]
internal readonly record struct GetExternalIPAddressRequestV2;