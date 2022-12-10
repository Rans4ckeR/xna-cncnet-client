using System;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.DXGUI.Generic;
using Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
#if DX || (GL && WINFORMS)
using System.Diagnostics;
using System.IO;
#endif
#if WINFORMS
using System.Windows.Forms;
using System.IO;
#endif

namespace DTAClient.DXGUI
{
    /// <summary>
    /// The main class for the game. Sets up asset search paths
    /// and initializes components.
    /// </summary>
    internal sealed class GameClass : Game
    {
        private readonly ILogger logger;
        private readonly UserINISettings userIniSettings;

        private ContentManager content;
        private WindowManager windowManager;
        //private LoadingScreen loadingScreen;
        private GraphicsDeviceManager graphics;

        public GameClass(ILogger logger, UserINISettings userIniSettings)
        {
            this.logger = logger;
            this.userIniSettings = userIniSettings;
        }

        public (WindowManager WindowManager, GraphicsDeviceManager GraphicsDeviceManager) SetupDependencies()
        {
            graphics = new GraphicsDeviceManager(this);
            windowManager = new(this, graphics);
            content = new ContentManager(Services);

            graphics.SynchronizeWithVerticalRetrace = false;
#if !XNA
            graphics.HardwareModeSwitch = false;
#endif

            return (windowManager, graphics);
        }

        //public void SetupLoadingScreen(LoadingScreen loadingScreen)
        //{
        //    this.loadingScreen = loadingScreen;
        //}

        protected override void Initialize()
        {
            logger.LogInformation("Initializing GameClass.");

            string windowTitle = ClientConfiguration.Instance.WindowTitle;
            Window.Title = string.IsNullOrEmpty(windowTitle) ?
                string.Format("{0} Client", ProgramConstants.GAME_NAME_SHORT) : windowTitle;

            string themePath = ClientConfiguration.Instance.GetThemePath(userIniSettings.ClientTheme)
                ?? ClientConfiguration.Instance.GetThemeInfoFromIndex(0)[1];
            ProgramConstants.RESOURCES_DIR = SafePath.CombineDirectoryPath(ProgramConstants.BASE_RESOURCE_PATH, themePath);

            AssetLoader.Initialize(GraphicsDevice, content);
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetBaseResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GamePath);

            base.Initialize();

#if DX || (GL && WINFORMS)
            // Try to create and load a texture to check for MonoGame compatibility
#if DX
            const string startupFailureFile = ".dxfail";
#elif GL && WINFORMS
            const string startupFailureFile = ".oglfail";
#endif

            try
            {
                Texture2D texture = new Texture2D(GraphicsDevice, 100, 100, false, SurfaceFormat.Color);
                Color[] colorArray = new Color[100 * 100];
                texture.SetData(colorArray);

                _ = AssetLoader.LoadTextureUncached("checkBoxClear.png");
            }
            catch (Exception ex) when (ex.Message.Contains("DeviceRemoved"))
            {
                logger.LogExceptionDetails(ex, $"Creating texture on startup failed! Creating {startupFailureFile} file and re-launching client launcher.");

                DirectoryInfo clientDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);

                if (!clientDirectory.Exists)
                    clientDirectory.Create();

                // Create startup failure file that the launcher can check for this error
                // and handle it by redirecting the user to another version instead
                File.WriteAllBytes(SafePath.CombineFilePath(clientDirectory.FullName, startupFailureFile), new byte[] { 1 });

                string launcherExe = ClientConfiguration.Instance.LauncherExe;
                if (string.IsNullOrEmpty(launcherExe))
                {
                    // LauncherExe is unspecified, just throw the exception forward
                    // because we can't handle it
                    logger.LogInformation("No LauncherExe= specified in ClientDefinitions.ini! " +
                        "Forwarding exception to regular exception handler.");

                    throw;
                }

                logger.LogInformation("Starting " + launcherExe + " and exiting.");

                Process.Start(SafePath.CombineFilePath(ProgramConstants.GamePath, launcherExe));
                Environment.Exit(1);
            }

#endif
            InitializeUISettings();

            windowManager.Initialize(content, ProgramConstants.GetBaseResourcePath());
            SetGraphicsMode(windowManager);
#if WINFORMS

            wm.SetIcon(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clienticon.ico"));
            wm.SetControlBox(true);
#endif

            windowManager.Cursor.Textures = new Texture2D[]
            {
                AssetLoader.LoadTexture("cursor.png"),
                AssetLoader.LoadTexture("waitCursor.png")
            };

#if WINFORMS
            FileInfo primaryNativeCursorPath = SafePath.GetFile(ProgramConstants.GetResourcePath(), "cursor.cur");
            FileInfo alternativeNativeCursorPath = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "cursor.cur");

            if (primaryNativeCursorPath.Exists)
                wm.Cursor.LoadNativeCursor(primaryNativeCursorPath.FullName);
            else if (alternativeNativeCursorPath.Exists)
                wm.Cursor.LoadNativeCursor(alternativeNativeCursorPath.FullName);

#endif
            Components.Add(windowManager);

            string playerName = userIniSettings.PlayerName.Value.Trim();

            if (userIniSettings.AutoRemoveUnderscoresFromName)
            {
                while (playerName.EndsWith("_"))
                    playerName = playerName[..^1];
            }

            if (string.IsNullOrEmpty(playerName))
            {
                playerName = Environment.UserName;

                playerName = playerName[(playerName.IndexOf("\\") + 1)..];
            }

            playerName = Renderer.GetSafeString(NameValidator.GetValidOfflineName(playerName), 0);

            ProgramConstants.PLAYERNAME = playerName;
            userIniSettings.PlayerName.Value = playerName;

            PreStartup.PostInitialize();
            // todo DI
            //windowManager.AddAndInitializeControl(loadingScreen);
            //loadingScreen.ClientRectangle = new Rectangle((windowManager.RenderResolutionX - loadingScreen.Width) / 2,
            //    (windowManager.RenderResolutionY - loadingScreen.Height) / 2, loadingScreen.Width, loadingScreen.Height);
        }

        private void InitializeUISettings()
        {
            UISettings settings = new UISettings();

            settings.AltColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIColor);
            settings.SubtleTextColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.UIHintTextColor);
            settings.ButtonTextColor = settings.AltColor;
            settings.ButtonHoverColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ButtonHoverColor);
            settings.TextColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.UILabelColor);
            settings.PanelBorderColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.PanelBorderColor);
            settings.BackgroundColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor);
            settings.FocusColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ListBoxFocusColor);
            settings.DisabledItemColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.DisabledButtonColor);

            settings.DefaultAlphaRate = ClientConfiguration.Instance.DefaultAlphaRate;
            settings.CheckBoxAlphaRate = ClientConfiguration.Instance.CheckBoxAlphaRate;
            settings.IndicatorAlphaRate = ClientConfiguration.Instance.IndicatorAlphaRate;

            settings.CheckBoxClearTexture = AssetLoader.LoadTexture("checkBoxClear.png");
            settings.CheckBoxCheckedTexture = AssetLoader.LoadTexture("checkBoxChecked.png");
            settings.CheckBoxDisabledClearTexture = AssetLoader.LoadTexture("checkBoxClearD.png");
            settings.CheckBoxDisabledCheckedTexture = AssetLoader.LoadTexture("checkBoxCheckedD.png");

            XNAPlayerSlotIndicator.LoadTextures();

            UISettings.ActiveSettings = settings;
        }

        /// <summary>
        /// Sets the client's graphics mode.
        /// TODO move to some helper class?
        /// </summary>
        /// <param name="wm">The window manager</param>
        public void SetGraphicsMode(WindowManager wm)
        {
            var clientConfiguration = ClientConfiguration.Instance;

            int windowWidth = userIniSettings.ClientResolutionX;
            int windowHeight = userIniSettings.ClientResolutionY;

            bool borderlessWindowedClient = userIniSettings.BorderlessWindowedClient;
            int currentWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int currentHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            if (currentWidth >= windowWidth && currentHeight >= windowHeight)
            {
                if (!wm.InitGraphicsMode(windowWidth, windowHeight, false))
                    throw new GraphicsModeInitializationException("Setting graphics mode failed!".L10N("UI:Main:SettingGraphicModeFailed") + " " + windowWidth + "x" + windowHeight);
            }
            else
            {
                if (!wm.InitGraphicsMode(1024, 600, false))
                    throw new GraphicsModeInitializationException("Setting default graphics mode failed!".L10N("UI:Main:SettingDefaultGraphicModeFailed"));
            }

            int renderResolutionX = 0;
            int renderResolutionY = 0;

            int initialXRes = Math.Max(windowWidth, clientConfiguration.MinimumRenderWidth);
            initialXRes = Math.Min(initialXRes, clientConfiguration.MaximumRenderWidth);

            int initialYRes = Math.Max(windowHeight, clientConfiguration.MinimumRenderHeight);
            initialYRes = Math.Min(initialYRes, clientConfiguration.MaximumRenderHeight);

            double xRatio = (windowWidth) / (double)initialXRes;
            double yRatio = (windowHeight) / (double)initialYRes;

            double ratio = xRatio > yRatio ? yRatio : xRatio;

            if ((windowWidth == 1366 || windowWidth == 1360) && windowHeight == 768)
            {
                renderResolutionX = windowWidth;
                renderResolutionY = windowHeight;
            }

            if (ratio > 1.0)
            {
                // Check whether we could sharp-scale our client window
                for (int i = 2; i < 10; i++)
                {
                    int sharpScaleRenderResX = windowWidth / i;
                    int sharpScaleRenderResY = windowHeight / i;

                    if (sharpScaleRenderResX >= clientConfiguration.MinimumRenderWidth &&
                        sharpScaleRenderResX <= clientConfiguration.MaximumRenderWidth &&
                        sharpScaleRenderResY >= clientConfiguration.MinimumRenderHeight &&
                        sharpScaleRenderResY <= clientConfiguration.MaximumRenderHeight)
                    {
                        renderResolutionX = sharpScaleRenderResX;
                        renderResolutionY = sharpScaleRenderResY;
                        break;
                    }
                }
            }

            if (renderResolutionX == 0 || renderResolutionY == 0)
            {
                renderResolutionX = initialXRes;
                renderResolutionY = initialYRes;

                if (ratio == xRatio)
                    renderResolutionY = (int)(windowHeight / ratio);
            }

            wm.SetBorderlessMode(borderlessWindowedClient);
#if !XNA

            if (borderlessWindowedClient)
            {
                graphics.IsFullScreen = true;
                graphics.ApplyChanges();
            }

#endif
            wm.CenterOnScreen();
            wm.SetRenderResolution(renderResolutionX, renderResolutionY);
        }
    }

    /// <summary>
    /// An exception that is thrown when initializing display / graphics mode fails.
    /// </summary>
    internal class GraphicsModeInitializationException : Exception
    {
        public GraphicsModeInitializationException(string message) : base(message)
        {
        }
    }
}