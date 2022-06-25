﻿using System;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

internal class PasswordRequestWindow : XNAWindow
{
    private readonly PrivateMessagingWindow privateMessagingWindow;

    private HostedCnCNetGame hostedGame;

    private bool pmWindowWasEnabled;

    private XNATextBox tbPassword;

    public PasswordRequestWindow(WindowManager windowManager, PrivateMessagingWindow privateMessagingWindow)
        : base(windowManager)
    {
        this.privateMessagingWindow = privateMessagingWindow;
    }

    public event EventHandler<PasswordEventArgs> PasswordEntered;

    public override void Initialize()
    {
        Name = "PasswordRequestWindow";
        BackgroundTexture = AssetLoader.LoadTexture("passwordquerybg.png");

        XNALabel lblDescription = new(WindowManager)
        {
            Name = "lblDescription",
            ClientRectangle = new Rectangle(12, 12, 0, 0),
            Text = "Please enter the password for the game and click OK.".L10N("UI:Main:EnterPasswordAndHitOK")
        };

        ClientRectangle = new Rectangle(0, 0, lblDescription.Width + 24, 110);

        tbPassword = new XNATextBox(WindowManager)
        {
            Name = "tbPassword",
            ClientRectangle = new Rectangle(
                lblDescription.X,
                lblDescription.Bottom + 12,
                Width - 24,
                21)
        };

        XNAClientButton btnOK = new(WindowManager)
        {
            Name = "btnOK",
            ClientRectangle = new Rectangle(
                lblDescription.X,
                ClientRectangle.Bottom - 35,
                UIDesignConstants.ButtonWidth92,
                UIDesignConstants.ButtonHeight),
            Text = "OK".L10N("UI:Main:ButtonOK")
        };
        btnOK.LeftClick += BtnOK_LeftClick;

        XNAClientButton btnCancel = new(WindowManager)
        {
            Name = "btnCancel",
            ClientRectangle = new Rectangle(
                Width - 104,
                btnOK.Y,
                UIDesignConstants.ButtonWidth92,
                UIDesignConstants.ButtonHeight),
            Text = "Cancel".L10N("UI:Main:ButtonCancel")
        };
        btnCancel.LeftClick += BtnCancel_LeftClick;

        AddChild(lblDescription);
        AddChild(tbPassword);
        AddChild(btnOK);
        AddChild(btnCancel);

        base.Initialize();

        CenterOnParent();

        EnabledChanged += PasswordRequestWindow_EnabledChanged;
        tbPassword.EnterPressed += TextBoxPassword_EnterPressed;
    }

    public void SetHostedGame(HostedCnCNetGame hostedGame)
    {
        this.hostedGame = hostedGame;
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Disable();
    }

    private void BtnOK_LeftClick(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(tbPassword.Text))
            return;

        pmWindowWasEnabled = false;
        Disable();

        PasswordEntered?.Invoke(this, new PasswordEventArgs(tbPassword.Text, hostedGame));
        tbPassword.Text = string.Empty;
    }

    private void PasswordRequestWindow_EnabledChanged(object sender, EventArgs e)
    {
        if (Enabled)
        {
            WindowManager.SelectedControl = tbPassword;
            if (!privateMessagingWindow.Enabled)
                return;
            pmWindowWasEnabled = true;
            privateMessagingWindow.Disable();
        }
        else if (pmWindowWasEnabled)
        {
            privateMessagingWindow.Enable();
        }
    }

    private void TextBoxPassword_EnterPressed(object sender, EventArgs eventArgs)
    {
        BtnOK_LeftClick(this, eventArgs);
    }
}