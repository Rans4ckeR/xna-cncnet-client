﻿using System;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Extensions;
using ClientGUI;
using ClientUpdater;
using DTAConfig.OptionPanels;
using Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig
{
    public sealed class OptionsWindow : XNAWindow
    {
        private readonly XNAMessageBox xnaMessageBox;
        private readonly UserINISettings userIniSettings;
        private readonly ComponentsPanel componentsPanel;
        private readonly UpdaterOptionsPanel updaterOptionsPanel;
        private readonly DisplayOptionsPanel displayOptionsPanel;
        private readonly AudioOptionsPanel audioOptionsPanel;
        private readonly GameOptionsPanel gameOptionsPanel;
        private readonly CnCNetOptionsPanel cncNetOptionsPanel;

        public OptionsWindow(
            WindowManager windowManager,
            GameCollection gameCollection,
            ILogger logger,
            XNAMessageBox xnaMessageBox,
            UserINISettings userIniSettings,
            ComponentsPanel componentsPanel,
            UpdaterOptionsPanel updaterOptionsPanel,
            DisplayOptionsPanel displayOptionsPanel,
            AudioOptionsPanel audioOptionsPanel,
            GameOptionsPanel gameOptionsPanel,
            CnCNetOptionsPanel cncNetOptionsPanel,
            IServiceProvider serviceProvider)
            : base(windowManager, logger, serviceProvider)
        {
            this.gameCollection = gameCollection;
            this.xnaMessageBox = xnaMessageBox;
            this.userIniSettings = userIniSettings;
            this.componentsPanel = componentsPanel;
            this.updaterOptionsPanel = updaterOptionsPanel;
            this.displayOptionsPanel = displayOptionsPanel;
            this.audioOptionsPanel = audioOptionsPanel;
            this.gameOptionsPanel = gameOptionsPanel;
            this.cncNetOptionsPanel = cncNetOptionsPanel;
        }

        public event EventHandler OnForceUpdate;

        private XNAClientTabControl tabControl;

        private XNAOptionsPanel[] optionsPanels;

        private XNAControl topBar;

        private GameCollection gameCollection;

        public override void Initialize()
        {
            Name = "OptionsWindow";
            ClientRectangle = new Rectangle(0, 0, 576, 475);
            BackgroundTexture = AssetLoader.LoadTextureUncached("optionsbg.png");

            tabControl = new XNAClientTabControl(WindowManager);
            tabControl.Name = "tabControl";
            tabControl.ClientRectangle = new Rectangle(12, 12, 0, 23);
            tabControl.FontIndex = 1;
            tabControl.ClickSound = new EnhancedSoundEffect("button.wav");
            tabControl.AddTab("Display".L10N("UI:DTAConfig:TabDisplay"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Audio".L10N("UI:DTAConfig:TabAudio"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Game".L10N("UI:DTAConfig:TabGame"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("CnCNet".L10N("UI:DTAConfig:TabCnCNet"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Updater".L10N("UI:DTAConfig:TabUpdater"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Components".L10N("UI:DTAConfig:TabComponents"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 104,
                Height - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("UI:DTAConfig:ButtonCancel");
            btnCancel.LeftClick += BtnBack_LeftClick;

            var btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = "btnSave";
            btnSave.ClientRectangle = new Rectangle(12, btnCancel.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnSave.Text = "Save".L10N("UI:DTAConfig:ButtonSave");
            btnSave.LeftClick += BtnSave_LeftClick;

            updaterOptionsPanel.OnForceUpdate += (s, e) => { Disable(); OnForceUpdate?.Invoke(this, EventArgs.Empty); };
            cncNetOptionsPanel.GameCollection = gameCollection;

            optionsPanels = new XNAOptionsPanel[]
            {
                displayOptionsPanel,
                audioOptionsPanel,
                gameOptionsPanel,
                cncNetOptionsPanel,
                updaterOptionsPanel,
                componentsPanel
            };

            if (ClientConfiguration.Instance.ModMode || Updater.UpdateMirrors == null || Updater.UpdateMirrors.Count < 1)
            {
                tabControl.MakeUnselectable(4);
                tabControl.MakeUnselectable(5);
            }
            else if (Updater.CustomComponents == null || Updater.CustomComponents.Count < 1)
                tabControl.MakeUnselectable(5);

            foreach (var panel in optionsPanels)
            {
                AddChild(panel);
                panel.Load();
                panel.Disable();
            }

            optionsPanels[0].Enable();

            AddChild(tabControl);
            AddChild(btnCancel);
            AddChild(btnSave);

            base.Initialize();

            CenterOnParent();
        }

        public void SetTopBar(XNAControl topBar) => this.topBar = topBar;

        /// <summary>
        /// Parses extra options defined by the modder
        /// from an INI file. Called from XNAWindow.SetAttributesFromINI.
        /// </summary>
        /// <param name="iniFile">The INI file.</param>
        protected override void GetINIAttributes(IniFile iniFile)
        {
            base.GetINIAttributes(iniFile);

            foreach (var panel in optionsPanels)
                panel.ParseUserOptions(iniFile);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (var panel in optionsPanels)
                panel.Disable();

            optionsPanels[tabControl.SelectedTab].Enable();
            optionsPanels[tabControl.SelectedTab].RefreshPanel();
        }

        private void BtnBack_LeftClick(object sender, EventArgs e)
        {
            if (Updater.IsComponentDownloadInProgress())
            {
                xnaMessageBox.Caption = "Downloads in progress".L10N("UI:DTAConfig:DownloadingTitle");
                xnaMessageBox.Description = ("Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu." +
                    Environment.NewLine + Environment.NewLine +
                    "Are you sure you want to continue?").L10N("UI:DTAConfig:DownloadingText");
                xnaMessageBox.MessageBoxButtons = XNAMessageBoxButtons.YesNo;
                xnaMessageBox.YesClickedAction = ExitDownloadCancelConfirmation_YesClicked;

                xnaMessageBox.Show();

                return;
            }

            WindowManager.SoundPlayer.SetVolume(Convert.ToSingle(userIniSettings.ClientVolume));
            Disable();
        }

        private void ExitDownloadCancelConfirmation_YesClicked(XNAMessageBox messageBox)
        {
            componentsPanel.CancelAllDownloads();
            WindowManager.SoundPlayer.SetVolume(Convert.ToSingle(userIniSettings.ClientVolume));
            Disable();
        }

        private void BtnSave_LeftClick(object sender, EventArgs e)
        {
            if (Updater.IsComponentDownloadInProgress())
            {
                xnaMessageBox.Caption = "Downloads in progress".L10N("UI:DTAConfig:DownloadingTitle");
                xnaMessageBox.Description = ("Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu." +
                      Environment.NewLine + Environment.NewLine +
                      "Are you sure you want to continue?").L10N("UI:DTAConfig:DownloadingText");
                xnaMessageBox.MessageBoxButtons = XNAMessageBoxButtons.YesNo;
                xnaMessageBox.YesClickedAction = SaveDownloadCancelConfirmation_YesClicked;

                xnaMessageBox.Show();

                return;
            }

            SaveSettings();
        }

        private void SaveDownloadCancelConfirmation_YesClicked(XNAMessageBox messageBox)
        {
            componentsPanel.CancelAllDownloads();

            SaveSettings();
        }

        private void SaveSettings()
        {
            if (RefreshOptionPanels())
                return;

            bool restartRequired = false;

            try
            {
                foreach (var panel in optionsPanels)
                    restartRequired = panel.Save() || restartRequired;

                userIniSettings.SaveSettings();
            }
            catch (Exception ex)
            {
                logger.LogExceptionDetails(ex, "Saving settings failed!");

                xnaMessageBox.Caption = "Saving Settings Failed".L10N("UI:DTAConfig:SaveSettingFailTitle");
                xnaMessageBox.Description = ("Saving settings failed! Error message:".L10N("UI:DTAConfig:SaveSettingFailText") + " " + ex.Message);
                xnaMessageBox.MessageBoxButtons = XNAMessageBoxButtons.OK;

                xnaMessageBox.Show();
            }

            Disable();

            if (restartRequired)
            {
                xnaMessageBox.Caption = "Restart Required".L10N("UI:DTAConfig:RestartClientTitle");
                xnaMessageBox.Description = ("The client needs to be restarted for some of the changes to take effect." +
                    Environment.NewLine + Environment.NewLine +
                    "Do you want to restart now?").L10N("UI:DTAConfig:RestartClientText");
                xnaMessageBox.MessageBoxButtons = XNAMessageBoxButtons.YesNo;
                xnaMessageBox.YesClickedAction = RestartMsgBox_YesClicked;

                xnaMessageBox.Show();
            }
        }

        private void RestartMsgBox_YesClicked(XNAMessageBox messageBox) => WindowManager.RestartGame();

        /// <summary>
        /// Refreshes the option panels to account for possible
        /// changes that could affect theirs functionality.
        /// Shows the popup to inform the user if needed.
        /// </summary>
        /// <returns>A bool that determines whether the
        /// settings values were changed.</returns>
        private bool RefreshOptionPanels()
        {
            bool optionValuesChanged = false;

            foreach (var panel in optionsPanels)
                optionValuesChanged = panel.RefreshPanel() || optionValuesChanged;

            if (optionValuesChanged)
            {
                xnaMessageBox.Caption = "Setting Value(s) Changed".L10N("UI:DTAConfig:SettingChangedTitle");
                xnaMessageBox.Description = ("One or more setting values are" + Environment.NewLine +
                    "no longer available and were changed." +
                    Environment.NewLine + Environment.NewLine +
                    "You may want to verify the new setting" + Environment.NewLine +
                    "values in client's options window.").L10N("UI:DTAConfig:SettingChangedText");
                xnaMessageBox.MessageBoxButtons = XNAMessageBoxButtons.OK;

                xnaMessageBox.Show();

                return true;
            }

            return false;
        }

        public void RefreshSettings()
        {
            foreach (var panel in optionsPanels)
                panel.Load();

            RefreshOptionPanels();

            foreach (var panel in optionsPanels)
                panel.Save();

            userIniSettings.SaveSettings();
        }

        public void Open()
        {
            foreach (var panel in optionsPanels)
                panel.Load();

            RefreshOptionPanels();

            componentsPanel.Open();

            Enable();
        }

        public void ToggleMainMenuOnlyOptions(bool enable)
        {
            foreach (var panel in optionsPanels)
            {
                panel.ToggleMainMenuOnlyOptions(enable);
            }
        }

        public void SwitchToCustomComponentsPanel()
        {
            foreach (var panel in optionsPanels)
                panel.Disable();

            tabControl.SelectedTab = 5;
        }

        public void InstallCustomComponent(int id) => componentsPanel.InstallComponent(id);

        public void PostInit()
        {
#if TS
            displayOptionsPanel.PostInit();
#endif
        }
    }
}