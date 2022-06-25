using System;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using Localization;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

/// <summary>
/// A window for selecting a CnCNet tunnel server.
/// </summary>
internal class TunnelSelectionWindow : XNAWindow
{
    private readonly TunnelHandler tunnelHandler;

    private XNAClientButton btnApply;

    private XNALabel lblDescription;

    private TunnelListBox lbTunnelList;

    private string originalTunnelAddress;

    public TunnelSelectionWindow(WindowManager windowManager, TunnelHandler tunnelHandler)
                        : base(windowManager)
    {
        this.tunnelHandler = tunnelHandler;
    }

    public event EventHandler<TunnelEventArgs> TunnelSelected;

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
        lblDescription.X = UIDesignConstants.EmptySpaceSides + UIDesignConstants.ControlHorizontalMargin;
        lblDescription.Y = UIDesignConstants.EmptySpaceTop + UIDesignConstants.ControlVerticalMargin;
        AddChild(lblDescription);

        lbTunnelList = new TunnelListBox(WindowManager, tunnelHandler);
        lbTunnelList.Name = nameof(lbTunnelList);
        lbTunnelList.Y = lblDescription.Bottom + UIDesignConstants.ControlVerticalMargin;
        lbTunnelList.X = UIDesignConstants.EmptySpaceSides + UIDesignConstants.ControlHorizontalMargin;
        AddChild(lbTunnelList);
        lbTunnelList.SelectedIndexChanged += LbTunnelList_SelectedIndexChanged;

        btnApply = new XNAClientButton(WindowManager);
        btnApply.Name = nameof(btnApply);
        btnApply.Width = UIDesignConstants.ButtonWidth92;
        btnApply.Height = UIDesignConstants.ButtonHeight;
        btnApply.Text = "Apply".L10N("UI:Main:ButtonApply");
        btnApply.X = UIDesignConstants.EmptySpaceSides + UIDesignConstants.ControlHorizontalMargin;
        btnApply.Y = lbTunnelList.Bottom + (UIDesignConstants.ControlVerticalMargin * 3);
        AddChild(btnApply);
        btnApply.LeftClick += BtnApply_LeftClick;

        XNAClientButton btnCancel = new(WindowManager);
        btnCancel.Name = nameof(btnCancel);
        btnCancel.Width = UIDesignConstants.ButtonWidth92;
        btnCancel.Height = UIDesignConstants.ButtonHeight;
        btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
        btnCancel.Y = btnApply.Y;
        AddChild(btnCancel);
        btnCancel.LeftClick += BtnCancel_LeftClick;

        Width = lbTunnelList.Right + UIDesignConstants.ControlHorizontalMargin + UIDesignConstants.EmptySpaceSides;
        Height = btnApply.Bottom + UIDesignConstants.ControlVerticalMargin + UIDesignConstants.EmptySpaceBottom;
        btnCancel.X = Width - btnCancel.Width - UIDesignConstants.EmptySpaceSides - UIDesignConstants.ControlHorizontalMargin;

        base.Initialize();
    }

    /// <summary>
    /// Sets the window's description and selects the tunnel server with the given address.
    /// </summary>
    /// <param name="description">The window description.</param>
    /// <param name="tunnelAddress">The address of the tunnel server to select.</param>
    public void Open(string description, string tunnelAddress = null)
    {
        lblDescription.Text = description;
        originalTunnelAddress = tunnelAddress;

        if (!string.IsNullOrWhiteSpace(tunnelAddress))
            lbTunnelList.SelectTunnel(tunnelAddress);
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
        btnApply.AllowClick = !lbTunnelList.IsTunnelSelected(originalTunnelAddress) && lbTunnelList.IsValidIndexSelected();
}
