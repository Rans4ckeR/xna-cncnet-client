﻿using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore.Extensions;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using ClientCore;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A window for selecting a CnCNet tunnel server.
    /// </summary>
    class TunnelSelectionWindow : XNAWindow
    {
        public TunnelSelectionWindow(WindowManager windowManager, TunnelHandler tunnelHandler) : base(windowManager)
        {
            this.tunnelHandler = tunnelHandler;
        }

        public event EventHandler<TunnelEventArgs> TunnelSelected;

        private readonly TunnelHandler tunnelHandler;
        private TunnelListBox lbTunnelList;
        private XNALabel lblDescription;
        private XNAClientButton btnApply;

        private string originalTunnelHash;

        public override void Initialize()
        {
            if (Initialized)
                return;

            Name = "TunnelSelectionWindow";

            BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = nameof(lblDescription);
            lblDescription.Text = "Line 1" + Environment.NewLine + "Line 2";
            lblDescription.X = UIDesignConstants.EMPTY_SPACE_SIDES + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            lblDescription.Y = UIDesignConstants.EMPTY_SPACE_TOP + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            AddChild(lblDescription);

            lbTunnelList = new TunnelListBox(WindowManager, tunnelHandler);
            lbTunnelList.Name = nameof(lbTunnelList);
            lbTunnelList.Y = lblDescription.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            lbTunnelList.X = UIDesignConstants.EMPTY_SPACE_SIDES + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            AddChild(lbTunnelList);
            lbTunnelList.SelectedIndexChanged += LbTunnelList_SelectedIndexChanged;

            btnApply = new XNAClientButton(WindowManager);
            btnApply.Name = nameof(btnApply);
            btnApply.Width = UIDesignConstants.BUTTON_WIDTH_92;
            btnApply.Height = UIDesignConstants.BUTTON_HEIGHT;
            btnApply.Text = "Apply".L10N("Client:Main:ButtonApply");
            btnApply.X = UIDesignConstants.EMPTY_SPACE_SIDES + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            btnApply.Y = lbTunnelList.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 3;
            AddChild(btnApply);
            btnApply.LeftClick += BtnApply_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Width = UIDesignConstants.BUTTON_WIDTH_92;
            btnCancel.Height = UIDesignConstants.BUTTON_HEIGHT;
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.Y = btnApply.Y;
            AddChild(btnCancel);
            btnCancel.LeftClick += BtnCancel_LeftClick;

            Width = lbTunnelList.Right + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN + UIDesignConstants.EMPTY_SPACE_SIDES;
            Height = btnApply.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN + UIDesignConstants.EMPTY_SPACE_BOTTOM;
            btnCancel.X = Width - btnCancel.Width - UIDesignConstants.EMPTY_SPACE_SIDES - UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;

            base.Initialize();
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            Disable();

            if (!lbTunnelList.IsValidIndexSelected())
                return;

            CnCNetTunnel tunnel = tunnelHandler.Tunnels[lbTunnelList.SelectedIndex];
            TunnelSelected?.Invoke(this, new TunnelEventArgs(tunnel));
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e) => Disable();

        private void LbTunnelList_SelectedIndexChanged(object sender, EventArgs e) =>
            btnApply.AllowClick = !lbTunnelList.IsTunnelSelected(originalTunnelHash) && lbTunnelList.IsValidIndexSelected();

        /// <summary>
        /// Sets the window's description and selects the tunnel server
        /// with the given address.
        /// </summary>
        /// <param name="description">The window description.</param>
        /// <param name="cnCNetTunnel">The tunnel server to select.</param>
        public void Open(string description, CnCNetTunnel cnCNetTunnel)
        {
            lblDescription.Text = description;
            originalTunnelHash = cnCNetTunnel?.Hash;
            lbTunnelList.CompatibilityFilter = cnCNetTunnel?.Version is ProgramConstants.TUNNEL_VERSION_2;

            if (cnCNetTunnel is not null)
                lbTunnelList.SelectTunnel(cnCNetTunnel);
            else
                lbTunnelList.SelectedIndex = -1;

            if (lbTunnelList.SelectedIndex > -1)
            {
                lbTunnelList.SetTopIndex(0);

                while (lbTunnelList.SelectedIndex > lbTunnelList.LastIndex)
                    lbTunnelList.TopIndex++;
            }

            btnApply.AllowClick = false;
            Enable();
        }
    }

    class TunnelEventArgs : EventArgs
    {
        public TunnelEventArgs(CnCNetTunnel tunnel)
        {
            Tunnel = tunnel;
        }

        public CnCNetTunnel Tunnel { get; }
    }
}