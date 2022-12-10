using System;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Extensions;
using ClientGUI;
using ClientUpdater;
using DTAClient.Domain.Multiplayer;
using DTAClient.Online;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Generic
{
    internal sealed class LoadingScreen : XNAWindow
    {
        private readonly UserINISettings userIniSettings;
        private readonly PrivacyNotification privacyNotification;
        private readonly MainMenu mainMenu;

        public LoadingScreen(
            CnCNetManager cncnetManager,
            WindowManager windowManager,
            MapLoader mapLoader,
            ILogger logger,
            UserINISettings userIniSettings,
            PrivacyNotification privacyNotification,
            IServiceProvider serviceProvider,
            MainMenu mainMenu) //todo DI
            : base(windowManager, logger, serviceProvider)
        {
            this.cncnetManager = cncnetManager;
            this.mapLoader = mapLoader;
            this.userIniSettings = userIniSettings;
            this.privacyNotification = privacyNotification;
            this.mainMenu = mainMenu;
        }

        private MapLoader mapLoader;
        private bool visibleSpriteCursor;
        private Task updaterInitTask;
        private Task mapLoadTask;
        private readonly CnCNetManager cncnetManager;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";

            BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

            base.Initialize();

            CenterOnParent();

            bool initUpdater = !ClientConfiguration.Instance.ModMode;

            if (initUpdater)
                updaterInitTask = Task.Run(InitUpdater).HandleTask();

            mapLoadTask = mapLoader.LoadMapsAsync().HandleTask();

            if (Cursor.Visible)
            {
                Cursor.Visible = false;
                visibleSpriteCursor = true;
            }
        }

        private void InitUpdater()
        {
            Updater.OnLocalFileVersionsChecked += LogGameClientVersion;
            Updater.CheckLocalFileVersions();
        }

        private void LogGameClientVersion()
        {
            logger.LogInformation($"Game Client Version: {ClientConfiguration.Instance.LocalGame} {Updater.GameVersion}");
            Updater.OnLocalFileVersionsChecked -= LogGameClientVersion;
        }

        private void Finish()
        {
            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ?
                "N/A" : Updater.GameVersion;

            // todo DI
            WindowManager.AddAndInitializeControl(mainMenu);
            mainMenu.PostInit();

            if (userIniSettings.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
            {
                cncnetManager.Connect();
            }

            if (!userIniSettings.PrivacyPolicyAccepted)
            {
                WindowManager.AddAndInitializeControl(privacyNotification);
            }

            WindowManager.RemoveControl(this);

            Cursor.Visible = visibleSpriteCursor;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion)
            {
                if (mapLoadTask.Status == TaskStatus.RanToCompletion)
                    Finish();
            }
        }
    }
}