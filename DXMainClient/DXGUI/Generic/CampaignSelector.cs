﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using ClientGUI;
using ClientUpdater;
using DTAClient.Domain;
using Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic
{
    internal sealed class CampaignSelector : XNAWindow
    {
        private const int DEFAULT_WIDTH = 650;
        private const int DEFAULT_HEIGHT = 600;

        private static string[] DifficultyNames = new string[] { "Easy", "Medium", "Hard" };
        private static string[] DifficultyIniPaths = new string[]
        {
            "INI/Map Code/Difficulty Easy.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Hard.ini"
        };

        private readonly UserINISettings userINISettings;
        private readonly GameProcessLogic gameProcessLogic;
        private readonly CheaterWindow cheaterWindow;
        private readonly DiscordHandler discordHandler;

        public CampaignSelector(
            WindowManager windowManager,
            DiscordHandler discordHandler,
            UserINISettings userINISettings,
            ILogger logger,
            GameProcessLogic gameProcessLogic,
            CheaterWindow cheaterWindow,
            IServiceProvider serviceProvider)
            : base(windowManager, logger, serviceProvider)
        {
            this.discordHandler = discordHandler;
            this.gameProcessLogic = gameProcessLogic;
            this.userINISettings = userINISettings;
            this.cheaterWindow = cheaterWindow;
        }

        private List<Mission> Missions = new List<Mission>();
        private XNAListBox lbCampaignList;
        private XNAClientButton btnLaunch;
        private XNATextBlock tbMissionDescription;
        private XNATrackbar trbDifficultySelector;

        private string[] filesToCheck = new string[]
        {
            "INI/AI.ini",
            "INI/AIE.ini",
            "INI/Art.ini",
            "INI/ArtE.ini",
            "INI/Enhance.ini",
            "INI/Rules.ini",
            "INI/Map Code/Difficulty Hard.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Easy.ini"
        };

        private Mission missionToLaunch;

        public override void Initialize()
        {
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;

            Name = "CampaignSelector";

            var lblSelectCampaign = new XNALabel(WindowManager);
            lblSelectCampaign.Name = "lblSelectCampaign";
            lblSelectCampaign.FontIndex = 1;
            lblSelectCampaign.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblSelectCampaign.Text = "MISSIONS:".L10N("UI:Main:Missions");

            lbCampaignList = new XNAListBox(WindowManager);
            lbCampaignList.Name = "lbCampaignList";
            lbCampaignList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            lbCampaignList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbCampaignList.ClientRectangle = new Rectangle(12,
                lblSelectCampaign.Bottom + 6, 300, 516);
            lbCampaignList.SelectedIndexChanged += LbCampaignList_SelectedIndexChanged;

            var lblMissionDescriptionHeader = new XNALabel(WindowManager);
            lblMissionDescriptionHeader.Name = "lblMissionDescriptionHeader";
            lblMissionDescriptionHeader.FontIndex = 1;
            lblMissionDescriptionHeader.ClientRectangle = new Rectangle(
                lbCampaignList.Right + 12,
                lblSelectCampaign.Y, 0, 0);
            lblMissionDescriptionHeader.Text = "MISSION DESCRIPTION:".L10N("UI:Main:MissionDescription");

            tbMissionDescription = new XNATextBlock(WindowManager);
            tbMissionDescription.Name = "tbMissionDescription";
            tbMissionDescription.ClientRectangle = new Rectangle(
                lblMissionDescriptionHeader.X,
                lblMissionDescriptionHeader.Bottom + 6,
                Width - 24 - lbCampaignList.Right, 430);
            tbMissionDescription.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            tbMissionDescription.Alpha = 1.0f;

            tbMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor),
                tbMissionDescription.Width, tbMissionDescription.Height);

            var lblDifficultyLevel = new XNALabel(WindowManager);
            lblDifficultyLevel.Name = "lblDifficultyLevel";
            lblDifficultyLevel.Text = "DIFFICULTY LEVEL".L10N("UI:Main:DifficultyLevel");
            lblDifficultyLevel.FontIndex = 1;
            Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
            lblDifficultyLevel.ClientRectangle = new Rectangle(
                tbMissionDescription.X + (tbMissionDescription.Width - (int)textSize.X) / 2,
                tbMissionDescription.Bottom + 12, (int)textSize.X, (int)textSize.Y);

            trbDifficultySelector = new XNATrackbar(WindowManager);
            trbDifficultySelector.Name = "trbDifficultySelector";
            trbDifficultySelector.ClientRectangle = new Rectangle(
                tbMissionDescription.X, lblDifficultyLevel.Bottom + 6,
                tbMissionDescription.Width, 30);
            trbDifficultySelector.MinValue = 0;
            trbDifficultySelector.MaxValue = 2;
            trbDifficultySelector.BackgroundTexture = AssetLoader.CreateTexture(
                new Color(0, 0, 0, 128), 2, 2);
            trbDifficultySelector.ButtonTexture = AssetLoader.LoadTextureUncached(
                "trackbarButton_difficulty.png");

            var lblEasy = new XNALabel(WindowManager);
            lblEasy.Name = "lblEasy";
            lblEasy.FontIndex = 1;
            lblEasy.Text = "EASY".L10N("UI:Main:DifficultyEasy");
            lblEasy.ClientRectangle = new Rectangle(trbDifficultySelector.X,
                trbDifficultySelector.Bottom + 6, 1, 1);

            var lblNormal = new XNALabel(WindowManager);
            lblNormal.Name = "lblNormal";
            lblNormal.FontIndex = 1;
            lblNormal.Text = "NORMAL".L10N("UI:Main:DifficultyNormal");
            textSize = Renderer.GetTextDimensions(lblNormal.Text, lblNormal.FontIndex);
            lblNormal.ClientRectangle = new Rectangle(
                tbMissionDescription.X + (tbMissionDescription.Width - (int)textSize.X) / 2,
                lblEasy.Y, (int)textSize.X, (int)textSize.Y);

            var lblHard = new XNALabel(WindowManager);
            lblHard.Name = "lblHard";
            lblHard.FontIndex = 1;
            lblHard.Text = "HARD".L10N("UI:Main:DifficultyHard");
            lblHard.ClientRectangle = new Rectangle(
                tbMissionDescription.Right - lblHard.Width,
                lblEasy.Y, 1, 1);

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = "btnLaunch";
            btnLaunch.ClientRectangle = new Rectangle(12, Height - 35, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLaunch.Text = "Launch".L10N("UI:Main:ButtonLaunch");
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += (_, _) => BtnLaunch_LeftClickAsync().HandleTask();

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 145,
                btnLaunch.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lblSelectCampaign);
            AddChild(lblMissionDescriptionHeader);
            AddChild(lbCampaignList);
            AddChild(tbMissionDescription);
            AddChild(lblDifficultyLevel);
            AddChild(btnLaunch);
            AddChild(btnCancel);
            AddChild(trbDifficultySelector);
            AddChild(lblEasy);
            AddChild(lblNormal);
            AddChild(lblHard);

            // Set control attributes from INI file
            base.Initialize();

            // Center on screen
            CenterOnParent();

            trbDifficultySelector.Value = userINISettings.Difficulty;

            ReadMissionList();

            var dp = new DarkeningPanel(WindowManager);
            dp.AddChild(cheaterWindow);
            AddChild(dp);
            dp.CenterOnParent();
            cheaterWindow.CenterOnParent();
            cheaterWindow.YesClicked += (_, _) => LaunchMissionAsync(missionToLaunch).HandleTask();
            cheaterWindow.Disable();
        }

        private void LbCampaignList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbCampaignList.SelectedIndex == -1)
            {
                tbMissionDescription.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            Mission mission = Missions[lbCampaignList.SelectedIndex];

            if (string.IsNullOrEmpty(mission.Scenario))
            {
                tbMissionDescription.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            tbMissionDescription.Text = mission.GUIDescription;

            if (!mission.Enabled)
            {
                btnLaunch.AllowClick = false;
                return;
            }

            btnLaunch.AllowClick = true;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }

        private async ValueTask BtnLaunch_LeftClickAsync()
        {
            int selectedMissionId = lbCampaignList.SelectedIndex;

            Mission mission = Missions[selectedMissionId];

            if (!ClientConfiguration.Instance.ModMode &&
                (!Updater.IsFileNonexistantOrOriginal(mission.Scenario) || AreFilesModified()))
            {
                // Confront the user by showing the cheater screen
                missionToLaunch = mission;
                cheaterWindow.Enable();
                return;
            }

            await LaunchMissionAsync(mission);
        }

        private bool AreFilesModified()
        {
            foreach (string filePath in filesToCheck)
            {
                if (!Updater.IsFileNonexistantOrOriginal(filePath))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Starts a singleplayer mission.
        /// </summary>
        private async ValueTask LaunchMissionAsync(Mission mission)
        {
            bool copyMapsToSpawnmapINI = ClientConfiguration.Instance.CopyMissionsToSpawnmapINI;

            logger.LogInformation($"About to write {ProgramConstants.SPAWNER_SETTINGS}.");
            using var spawnStreamWriter = new StreamWriter(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS));
            spawnStreamWriter.WriteLine("; Generated by DTA Client");
            spawnStreamWriter.WriteLine("[Settings]");
            if (copyMapsToSpawnmapINI)
                spawnStreamWriter.WriteLine($"Scenario={ProgramConstants.SPAWNMAP_INI}");
            else
                spawnStreamWriter.WriteLine("Scenario=" + mission.Scenario);

            // No one wants to play missions on Fastest, so we'll change it to Faster
            if (userINISettings.GameSpeed == 0)
                userINISettings.GameSpeed.Value = 1;

            spawnStreamWriter.WriteLine("CampaignID=" + mission.Index);
            spawnStreamWriter.WriteLine("GameSpeed=" + userINISettings.GameSpeed);
            spawnStreamWriter.WriteLine("Firestorm=" + mission.RequiredAddon);

            int resHeight = userINISettings.IngameScreenHeight;

            spawnStreamWriter.WriteLine("CustomLoadScreen=" + LoadingScreenController.GetLoadScreenName(mission.Side.ToString(), resHeight));
            spawnStreamWriter.WriteLine("IsSinglePlayer=Yes");
            spawnStreamWriter.WriteLine("SidebarHack=" + ClientConfiguration.Instance.SidebarHack);
            spawnStreamWriter.WriteLine("Side=" + mission.Side);
            spawnStreamWriter.WriteLine("BuildOffAlly=" + mission.BuildOffAlly);

            userINISettings.Difficulty.Value = trbDifficultySelector.Value;

            spawnStreamWriter.WriteLine("DifficultyModeHuman=" + (mission.PlayerAlwaysOnNormalDifficulty ? "1" : trbDifficultySelector.Value.ToString()));
            spawnStreamWriter.WriteLine("DifficultyModeComputer=" + GetComputerDifficulty());

            var difficultyIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[trbDifficultySelector.Value]));
            string difficultyName = DifficultyNames[trbDifficultySelector.Value];

            spawnStreamWriter.WriteLine();
            spawnStreamWriter.WriteLine();
            spawnStreamWriter.WriteLine();

            if (copyMapsToSpawnmapINI)
            {
                var mapIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, mission.Scenario));
                IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
                mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SPAWNMAP_INI));
            }

            userINISettings.Difficulty.Value = trbDifficultySelector.Value;
            userINISettings.SaveSettings();

            ((MainMenuDarkeningPanel)Parent).Hide();

            discordHandler.UpdatePresence(mission.GUIName, difficultyName, mission.IconPath, true);
            gameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            await gameProcessLogic.StartGameProcessAsync();
        }

        private int GetComputerDifficulty() =>
            Math.Abs(trbDifficultySelector.Value - 2);

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(GameProcessExited);
        }

        private void GameProcessExited()
        {
            gameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
            discordHandler.UpdatePresence();
        }

        private void ReadMissionList()
        {
            ParseBattleIni("INI/Battle.ini");

            if (Missions.Count == 0)
                ParseBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);
        }

        /// <summary>
        /// Parses a Battle(E).ini file. Returns true if succesful (file found), otherwise false.
        /// </summary>
        /// <param name="path">The path of the file, relative to the game directory.</param>
        /// <returns>True if succesful, otherwise false.</returns>
        private bool ParseBattleIni(string path)
        {
            logger.LogInformation("Attempting to parse " + path + " to populate mission list.");

            FileInfo battleIniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);
            if (!battleIniFileInfo.Exists)
            {
                logger.LogInformation("File " + path + " not found. Ignoring.");
                return false;
            }

            if (Missions.Count > 0)
            {
                throw new InvalidOperationException("Loading multiple Battle*.ini files is not supported anymore.");
            }

            var battleIni = new IniFile(battleIniFileInfo.FullName);

            List<string> battleKeys = battleIni.GetSectionKeys("Battles");

            if (battleKeys == null)
                return false; // File exists but [Battles] doesn't

            for (int i = 0; i < battleKeys.Count; i++)
            {
                string battleEntry = battleKeys[i];
                string battleSection = battleIni.GetStringValue("Battles", battleEntry, "NOT FOUND");

                if (!battleIni.SectionExists(battleSection))
                    continue;

                var mission = new Mission(battleIni, battleSection, i);

                Missions.Add(mission);

                var item = new XNAListBoxItem();
                item.Text = mission.GUIName;
                if (!mission.Enabled)
                {
                    item.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                }
                else if (string.IsNullOrEmpty(mission.Scenario))
                {
                    item.TextColor = AssetLoader.GetColorFromString(
                        ClientConfiguration.Instance.ListBoxHeaderColor);
                    item.IsHeader = true;
                    item.Selectable = false;
                }
                else
                {
                    item.TextColor = lbCampaignList.DefaultItemColor;
                }

                if (!string.IsNullOrEmpty(mission.IconPath))
                    item.Texture = AssetLoader.LoadTexture(mission.IconPath + "icon.png");

                lbCampaignList.AddItem(item);
            }

            logger.LogInformation("Finished parsing " + path + ".");
            return true;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}