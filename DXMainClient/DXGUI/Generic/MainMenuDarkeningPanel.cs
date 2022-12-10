using System;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// TODO Replace this class with DarkeningPanels.
    /// Handles transitions between the main menu and its sub-menus.
    /// </summary>
    internal sealed class MainMenuDarkeningPanel : XNAPanel
    {
        private readonly GameLoadingWindow gameLoadingWindow;
        private readonly CampaignSelector campaignSelector;

        public MainMenuDarkeningPanel(
            WindowManager windowManager,
            GameLoadingWindow gameLoadingWindow,
            CampaignSelector campaignSelector,
            ExtrasWindow extrasWindow,
            UpdateWindow updateWindow,
            StatisticsWindow statisticsWindow,
            UpdateQueryWindow updateQueryWindow,
            ManualUpdateQueryWindow manualUpdateQueryWindow)
            : base(windowManager)
        {
            this.gameLoadingWindow = gameLoadingWindow;
            this.campaignSelector = campaignSelector;
            StatisticsWindow = statisticsWindow;
            UpdateWindow = updateWindow;
            UpdateQueryWindow = updateQueryWindow;
            ManualUpdateQueryWindow = manualUpdateQueryWindow;
            DrawBorders = false;
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            ExtrasWindow = extrasWindow;
        }

        public CampaignSelector CampaignSelector;
        public GameLoadingWindow GameLoadingWindow;
        public StatisticsWindow StatisticsWindow;
        public UpdateQueryWindow UpdateQueryWindow;
        public ManualUpdateQueryWindow ManualUpdateQueryWindow;
        public UpdateWindow UpdateWindow;
        public ExtrasWindow ExtrasWindow;

        public override void Initialize()
        {
            base.Initialize();

            Name = "DarkeningPanel";
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            Alpha = 1.0f;

            CampaignSelector = campaignSelector;
            AddChild(CampaignSelector);

            GameLoadingWindow = gameLoadingWindow;
            AddChild(GameLoadingWindow);
            AddChild(StatisticsWindow);
            AddChild(UpdateQueryWindow);
            AddChild(ManualUpdateQueryWindow);

            AddChild(UpdateWindow);
            AddChild(ExtrasWindow);

            foreach (XNAControl child in Children)
            {
                child.Visible = false;
                child.Enabled = false;
                child.EnabledChanged += Child_EnabledChanged;
            }
        }

        private void Child_EnabledChanged(object sender, EventArgs e)
        {
            XNAWindow child = (XNAWindow)sender;
            if (!child.Enabled)
                Hide();
        }

        public void Show(XNAControl control)
        {
            foreach (XNAControl child in Children)
            {
                child.Enabled = false;
                child.Visible = false;
            }

            Enabled = true;
            Visible = true;

            AlphaRate = DarkeningPanel.ALPHA_RATE;

            if (control != null)
            {
                control.Enabled = true;
                control.Visible = true;
                control.IgnoreInputOnFrame = true;
            }
        }

        public void Hide()
        {
            AlphaRate = -DarkeningPanel.ALPHA_RATE;

            foreach (XNAControl child in Children)
            {
                child.Enabled = false;
                child.Visible = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Alpha <= 0f)
            {
                Enabled = false;
                Visible = false;

                foreach (XNAControl child in Children)
                {
                    child.Visible = false;
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            DrawTexture(BackgroundTexture, Point.Zero, Color.White);
            base.Draw(gameTime);
        }
    }
}