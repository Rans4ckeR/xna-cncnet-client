using System;
using DTAClient.Domain.Multiplayer.CnCNet;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

internal class TunnelEventArgs : EventArgs
{
    public TunnelEventArgs(CnCNetTunnel tunnel)
    {
        Tunnel = tunnel;
    }

    public CnCNetTunnel Tunnel { get; }
}