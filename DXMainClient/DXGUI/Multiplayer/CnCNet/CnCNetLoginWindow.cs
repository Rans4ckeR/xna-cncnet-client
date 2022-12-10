﻿using System;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    internal sealed class CnCNetLoginWindow : XNAWindow
    {
        private readonly UserINISettings userIniSettings;
        private readonly XNAMessageBox xnaMessageBox;

        public CnCNetLoginWindow(
            WindowManager windowManager,
            ILogger logger,
            UserINISettings userIniSettings,
            XNAMessageBox xnaMessageBox,
            IServiceProvider serviceProvider)
            : base(windowManager, logger, serviceProvider)
        {
            this.userIniSettings = userIniSettings;
            this.xnaMessageBox = xnaMessageBox;
        }

        private XNALabel lblConnectToCnCNet;
        private XNATextBox tbPlayerName;
        private XNALabel lblPlayerName;
        private XNAClientCheckBox chkRememberMe;
        private XNAClientCheckBox chkPersistentMode;
        private XNAClientCheckBox chkAutoConnect;
        private XNAClientButton btnConnect;
        private XNAClientButton btnCancel;

        public event EventHandler Cancelled;
        public event EventHandler Connect;

        public override void Initialize()
        {
            Name = "CnCNetLoginWindow";
            ClientRectangle = new Rectangle(0, 0, 300, 220);
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");

            lblConnectToCnCNet = new XNALabel(WindowManager);
            lblConnectToCnCNet.Name = "lblConnectToCnCNet";
            lblConnectToCnCNet.FontIndex = 1;
            lblConnectToCnCNet.Text = "CONNECT TO CNCNET".L10N("UI:Main:ConnectToCncNet");

            AddChild(lblConnectToCnCNet);
            lblConnectToCnCNet.CenterOnParent();
            lblConnectToCnCNet.ClientRectangle = new Rectangle(
                lblConnectToCnCNet.X, 12,
                lblConnectToCnCNet.Width,
                lblConnectToCnCNet.Height);

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = "tbPlayerName";
            tbPlayerName.ClientRectangle = new Rectangle(Width - 132, 50, 120, 19);
            tbPlayerName.MaximumTextLength = ClientConfiguration.Instance.MaxNameLength;
            string defgame = ClientConfiguration.Instance.LocalGame;

            lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = "lblPlayerName";
            lblPlayerName.FontIndex = 1;
            lblPlayerName.Text = "PLAYER NAME:".L10N("UI:Main:PlayerName");
            lblPlayerName.ClientRectangle = new Rectangle(12, tbPlayerName.Y + 1,
                lblPlayerName.Width, lblPlayerName.Height);

            chkRememberMe = new XNAClientCheckBox(WindowManager);
            chkRememberMe.Name = "chkRememberMe";
            chkRememberMe.ClientRectangle = new Rectangle(12, tbPlayerName.Bottom + 12, 0, 0);
            chkRememberMe.Text = "Remember me".L10N("UI:Main:RememberMe");
            chkRememberMe.TextPadding = 7;
            chkRememberMe.CheckedChanged += ChkRememberMe_CheckedChanged;

            chkPersistentMode = new XNAClientCheckBox(WindowManager);
            chkPersistentMode.Name = "chkPersistentMode";
            chkPersistentMode.ClientRectangle = new Rectangle(12, chkRememberMe.Bottom + 30, 0, 0);
            chkPersistentMode.Text = "Stay connected outside of the CnCNet lobby".L10N("UI:Main:StayConnect");
            chkPersistentMode.TextPadding = chkRememberMe.TextPadding;
            chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;

            chkAutoConnect = new XNAClientCheckBox(WindowManager);
            chkAutoConnect.Name = "chkAutoConnect";
            chkAutoConnect.ClientRectangle = new Rectangle(12, chkPersistentMode.Bottom + 30, 0, 0);
            chkAutoConnect.Text = "Connect automatically on client startup".L10N("UI:Main:AutoConnect");
            chkAutoConnect.TextPadding = chkRememberMe.TextPadding;
            chkAutoConnect.AllowChecking = false;

            btnConnect = new XNAClientButton(WindowManager);
            btnConnect.Name = "btnConnect";
            btnConnect.ClientRectangle = new Rectangle(12, Height - 35, 110, 23);
            btnConnect.Text = "Connect".L10N("UI:Main:ButtonConnect");
            btnConnect.LeftClick += BtnConnect_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 122, btnConnect.Y, 110, 23);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(tbPlayerName);
            AddChild(lblPlayerName);
            AddChild(chkRememberMe);
            AddChild(chkPersistentMode);
            AddChild(chkAutoConnect);
            AddChild(btnConnect);
            AddChild(btnCancel);

            base.Initialize();

            CenterOnParent();

            userIniSettings.SettingsSaved += Instance_SettingsSaved;
        }

        private void Instance_SettingsSaved(object sender, EventArgs e)
        {
            tbPlayerName.Text = userIniSettings.PlayerName;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void ChkRememberMe_CheckedChanged(object sender, EventArgs e)
        {
            CheckAutoConnectAllowance();
        }

        private void ChkPersistentMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckAutoConnectAllowance();
        }

        private void CheckAutoConnectAllowance()
        {
            chkAutoConnect.AllowChecking = chkPersistentMode.Checked && chkRememberMe.Checked;
            if (!chkAutoConnect.AllowChecking)
                chkAutoConnect.Checked = false;
        }

        private void BtnConnect_LeftClick(object sender, EventArgs e)
        {
            string errorMessage = NameValidator.IsNameValid(tbPlayerName.Text);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                xnaMessageBox.Caption = "Invalid Player Name".L10N("UI:Main:InvalidPlayerName");
                xnaMessageBox.Description = errorMessage;
                xnaMessageBox.MessageBoxButtons = XNAMessageBoxButtons.OK;

                xnaMessageBox.Show();
                return;
            }

            ProgramConstants.PLAYERNAME = tbPlayerName.Text;

            userIniSettings.SkipConnectDialog.Value = chkRememberMe.Checked;
            userIniSettings.PersistentMode.Value = chkPersistentMode.Checked;
            userIniSettings.AutomaticCnCNetLogin.Value = chkAutoConnect.Checked;
            userIniSettings.PlayerName.Value = ProgramConstants.PLAYERNAME;

            userIniSettings.SaveSettings();

            Connect?.Invoke(this, EventArgs.Empty);
        }

        public void LoadSettings()
        {
            chkAutoConnect.Checked = userIniSettings.AutomaticCnCNetLogin;
            chkPersistentMode.Checked = userIniSettings.PersistentMode;
            chkRememberMe.Checked = userIniSettings.SkipConnectDialog;

            tbPlayerName.Text = userIniSettings.PlayerName;

            if (chkRememberMe.Checked)
                BtnConnect_LeftClick(this, EventArgs.Empty);
        }
    }
}