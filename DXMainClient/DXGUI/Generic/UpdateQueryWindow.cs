using System;
using System.Diagnostics;
using ClientCore;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic;

/// <summary>
/// A window that asks the user whether they want to update their game.
/// </summary>
public class UpdateQueryWindow : XNAWindow
{
    private string changelogUrl;
    private XNALabel lblDescription;

    private XNALabel lblUpdateSize;

    public UpdateQueryWindow(WindowManager windowManager)
            : base(windowManager)
    {
    }

    public event EventHandler UpdateAccepted;

    public event EventHandler UpdateDeclined;

    public override void Initialize()
    {
        changelogUrl = ClientConfiguration.Instance.ChangelogURL;

        Name = "UpdateQueryWindow";
        ClientRectangle = new Rectangle(0, 0, 251, 140);
        BackgroundTexture = AssetLoader.LoadTexture("updatequerybg.png");

        lblDescription = new XNALabel(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 9, 0, 0),
            Text = string.Empty
        };
        lblDescription.Name = nameof(lblDescription);

        XNALinkLabel lblChangelogLink = new(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 50, 0, 0),
            Text = "View Changelog".L10N("UI:Main:ViewChangeLog"),
            IdleColor = Color.Goldenrod
        };
        lblChangelogLink.Name = nameof(lblChangelogLink);
        lblChangelogLink.LeftClick += LblChangelogLink_LeftClick;

        lblUpdateSize = new XNALabel(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 80, 0, 0),
            Text = string.Empty
        };
        lblUpdateSize.Name = nameof(lblUpdateSize);

        XNAClientButton btnYes = new(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 110, 75, 23),
            Text = "Yes".L10N("UI:Main:ButtonYes")
        };
        btnYes.LeftClick += BtnYes_LeftClick;
        btnYes.Name = nameof(btnYes);

        XNAClientButton btnNo = new(WindowManager)
        {
            ClientRectangle = new Rectangle(164, 110, 75, 23),
            Text = "No".L10N("UI:Main:ButtonNo")
        };
        btnNo.LeftClick += BtnNo_LeftClick;
        btnNo.Name = nameof(btnNo);

        AddChild(lblDescription);
        AddChild(lblChangelogLink);
        AddChild(lblUpdateSize);
        AddChild(btnYes);
        AddChild(btnNo);

        base.Initialize();

        CenterOnParent();
    }

    public void SetInfo(string version, int updateSize)
    {
        lblDescription.Text = string.Format(("Version {0} is available for download." + Environment.NewLine + "Do you wish to install it?").L10N("UI:Main:VersionAvailable"), version);
        lblUpdateSize.Text = updateSize >= 1000
            ? string.Format("The size of the update is {0} MB.".L10N("UI:Main:UpdateSizeMB"), updateSize / 1000)
            : string.Format("The size of the update is {0} KB.".L10N("UI:Main:UpdateSizeKB"), updateSize);
    }

    private void BtnNo_LeftClick(object sender, EventArgs e)
    {
        UpdateDeclined?.Invoke(this, e);
    }

    private void BtnYes_LeftClick(object sender, EventArgs e)
    {
        UpdateAccepted?.Invoke(this, e);
    }

    private void LblChangelogLink_LeftClick(object sender, EventArgs e)
    {
        using Process proc = Process.Start(new ProcessStartInfo
        {
            FileName = changelogUrl,
            UseShellExecute = true
        });
    }
}