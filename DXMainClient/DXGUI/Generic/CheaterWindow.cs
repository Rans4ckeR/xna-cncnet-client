using System;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic;

public class CheaterWindow : XNAWindow
{
    public CheaterWindow(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public event EventHandler YesClicked;

    public override void Initialize()
    {
        Name = "CheaterScreen";
        ClientRectangle = new Rectangle(0, 0, 334, 453);
        BackgroundTexture = AssetLoader.LoadTexture("cheaterbg.png");

        XNALabel lblCheater = new(WindowManager)
        {
            Name = "lblCheater",
            ClientRectangle = new Rectangle(0, 0, 0, 0),
            FontIndex = 1,
            Text = "CHEATER!".L10N("UI:Main:Cheater")
        };

        XNALabel lblDescription = new(WindowManager)
        {
            Name = "lblDescription",
            ClientRectangle = new Rectangle(12, 40, 0, 0),
            Text = ("Modified game files have been detected. They could affect" + Environment.NewLine +
            "the game experience." +
            Environment.NewLine + Environment.NewLine +
            "Do you really lack the skill for winning the mission without" + Environment.NewLine + "cheating?").L10N("UI:Main:CheaterText")
        };

        XNAPanel imagePanel = new(WindowManager)
        {
            Name = "imagePanel",
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED,
            ClientRectangle = new Rectangle(
                lblDescription.X,
            lblDescription.Bottom + 12, Width - 24,
            Height - (lblDescription.Bottom + 59)),
            BackgroundTexture = AssetLoader.LoadTextureUncached("cheater.png")
        };

        XNAClientButton btnCancel = new(WindowManager)
        {
            Name = "btnCancel",
            ClientRectangle = new Rectangle(
                Width - 104,
            Height - 35, UIDesignConstants.BUTTONWIDTH92, UIDesignConstants.BUTTONHEIGHT),
            Text = "Cancel".L10N("UI:Main:ButtonCancel")
        };
        btnCancel.LeftClick += BtnCancel_LeftClick;

        XNAClientButton btnYes = new(WindowManager)
        {
            Name = "btnYes",
            ClientRectangle = new Rectangle(12, btnCancel.Y,
            btnCancel.Width, btnCancel.Height),
            Text = "Yes".L10N("UI:Main:ButtonYes")
        };
        btnYes.LeftClick += BtnYes_LeftClick;

        AddChild(lblCheater);
        AddChild(lblDescription);
        AddChild(imagePanel);
        AddChild(btnCancel);
        AddChild(btnYes);

        lblCheater.CenterOnParent();
        lblCheater.ClientRectangle = new Rectangle(lblCheater.X, 12,
            lblCheater.Width, lblCheater.Height);

        base.Initialize();
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Disable();
    }

    private void BtnYes_LeftClick(object sender, EventArgs e)
    {
        Disable();
        YesClicked?.Invoke(this, EventArgs.Empty);
    }
}