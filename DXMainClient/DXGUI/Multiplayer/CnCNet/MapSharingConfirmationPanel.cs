using System;
using ClientGUI;
using Localization;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

/// <summary>
/// A panel that is used to verify and display map sharing status.
/// </summary>
internal class MapSharingConfirmationPanel : XNAPanel
{
    private readonly string mapSharingDownloadText =
        "Downloading map...".L10N("UI:Main:MapSharingDownloadText");

    private readonly string mapSharingFailedText =
        ("Downloading map failed. The game host" + Environment.NewLine +
        "needs to change the map or you will be" + Environment.NewLine +
        "unable to participate in the match.").L10N("UI:Main:MapSharingFailedText");

    private readonly string mapSharingRequestText =
                ("The game host has selected a map that" + Environment.NewLine +
        "doens't exist on your local installation.").L10N("UI:Main:MapSharingRequestText");

    private XNAClientButton btnDownload;

    private XNALabel lblDescription;

    public MapSharingConfirmationPanel(WindowManager windowManager)
                : base(windowManager)
    {
    }

    public event EventHandler MapDownloadConfirmed;

    public override void Initialize()
    {
        PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;

        Name = nameof(MapSharingConfirmationPanel);
        BackgroundTexture = AssetLoader.LoadTexture("msgboxform.png");

        lblDescription = new XNALabel(WindowManager);
        lblDescription.Name = nameof(lblDescription);
        lblDescription.X = UIDesignConstants.EmptySpaceSides;
        lblDescription.Y = UIDesignConstants.EmptySpaceTop;
        lblDescription.Text = mapSharingRequestText;
        AddChild(lblDescription);

        Width = lblDescription.Right + UIDesignConstants.EmptySpaceSides;

        btnDownload = new XNAClientButton(WindowManager);
        btnDownload.Name = nameof(btnDownload);
        btnDownload.Width = UIDesignConstants.ButtonWidth92;
        btnDownload.Y = lblDescription.Bottom + (UIDesignConstants.EmptySpaceTop * 2);
        btnDownload.Text = "Download".L10N("UI:Main:ButtonDownload");
        btnDownload.LeftClick += (s, e) => MapDownloadConfirmed?.Invoke(this, EventArgs.Empty);
        AddChild(btnDownload);
        btnDownload.CenterOnParentHorizontally();

        Height = btnDownload.Bottom + UIDesignConstants.EmptySpaceBottom;

        base.Initialize();

        CenterOnParent();

        Disable();
    }

    public void SetDownloadingStatus()
    {
        lblDescription.Text = mapSharingDownloadText;
        btnDownload.AllowClick = false;
    }

    public void SetFailedStatus()
    {
        lblDescription.Text = mapSharingFailedText;
        btnDownload.AllowClick = false;
    }

    public void ShowForMapDownload()
    {
        lblDescription.Text = mapSharingRequestText;
        btnDownload.AllowClick = true;
        Enable();
    }
}