using System;
#if WINFORMS
using System.Windows.Forms;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Extensions;
using ClientCore.INIProcessing;
using ClientCore.Settings;
using ClientCore.Statistics;
using ClientCore.Statistics.GameParsers;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using DTAConfig;
using DTAConfig.OptionPanels;
using DTAConfig.Settings;
using Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient
{
    /// <summary>
    /// Contains client startup parameters.
    /// </summary>
    internal struct StartupParams
    {
        public StartupParams(bool noAudio, bool multipleInstanceMode,
            List<string> unknownParams)
        {
            NoAudio = noAudio;
            MultipleInstanceMode = multipleInstanceMode;
            UnknownStartupParams = unknownParams;
        }

        public bool NoAudio { get; }
        public bool MultipleInstanceMode { get; }
        public List<string> UnknownStartupParams { get; }
    }

    internal static class PreStartup
    {
        private static ILogger logger;
        private static IServiceProvider serviceProvider;
        private static StartupParams startupParams;

        /// <summary>
        /// Initializes various basic systems like the client's logger,
        /// constants, and the general exception handler.
        /// Reads the user's settings from an INI file,
        /// checks for necessary permissions and starts the client if
        /// everything goes as it should.
        /// </summary>
        /// <param name="parameters">The client's startup parameters.</param>
        public static void Initialize(StartupParams parameters)
        {
#if WINFORMS
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.ThreadException += (_, args) => ProgramConstants.HandleException(args.Exception);
#endif
            startupParams = parameters;
            DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);

            Environment.CurrentDirectory = gameDirectory.FullName;

            DirectoryInfo clientUserFilesDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);
            FileInfo clientLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client.log");
            ProgramConstants.LogFileName = clientLogFile.FullName;

            Rampastring.Tools.Logger.Initialize(clientUserFilesDirectory.FullName, clientLogFile.Name);
            Rampastring.Tools.Logger.WriteLogFile = true;

            IServiceProvider startupServiceProvider = BuildStartupServiceProvider();

            logger = startupServiceProvider.GetService<ILogger>();
            ErrorHandler errorHandler = startupServiceProvider.GetService<ErrorHandler>();

            AppDomain.CurrentDomain.UnhandledException += (_, args) => errorHandler.HandleException((Exception)args.ExceptionObject);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CheckPermissions(errorHandler);

            serviceProvider = BuildServiceProvider(startupServiceProvider);
            GameClass gameClass = serviceProvider.GetService<GameClass>();

            //gameClass.SetupLoadingScreen(serviceProvider.GetService<LoadingScreen>());
            UserINISettings userIniSettings = serviceProvider.GetService<UserINISettings>();

            int currentWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int currentHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            userIniSettings.ClientResolutionX = new IntSetting(userIniSettings.SettingsIni, UserINISettings.VIDEO, "ClientResolutionX", currentWidth);
            userIniSettings.ClientResolutionY = new IntSetting(userIniSettings.SettingsIni, UserINISettings.VIDEO, "ClientResolutionY", currentHeight);

            gameClass.Run();

            //            LoadingScreen loadingScreen = serviceProvider.GetService<LoadingScreen>();
            //            WindowManager windowManager = serviceProvider.GetService<WindowManager>();
            //            windowManager.AddAndInitializeControl(loadingScreen);
            //            loadingScreen.ClientRectangle = new Rectangle((windowManager.RenderResolutionX - loadingScreen.Width) / 2,
            //                (windowManager.RenderResolutionY - loadingScreen.Height) / 2, loadingScreen.Width, loadingScreen.Height);

            //            GameCollection gameCollection = serviceProvider.GetService<GameCollection>();

            //            gameCollection.Initialize();

            //            if (!clientUserFilesDirectory.Exists)
            //                clientUserFilesDirectory.Create();

            //            clientLogFile.Delete();

            //            ProgramConstants.OSId = ClientConfiguration.Instance.GetOperatingSystemVersion();
            //            ProgramConstants.GAME_NAME_SHORT = ClientConfiguration.Instance.LocalGame;
            //            ProgramConstants.GAME_NAME_LONG = ClientConfiguration.Instance.LongGameName;
            //            ProgramConstants.SUPPORT_URL_SHORT = ClientConfiguration.Instance.ShortSupportURL;
            //            ProgramConstants.CREDITS_URL = ClientConfiguration.Instance.CreditsURL;
            //            ProgramConstants.MAP_CELL_SIZE_X = ClientConfiguration.Instance.MapCellSizeX;
            //            ProgramConstants.MAP_CELL_SIZE_Y = ClientConfiguration.Instance.MapCellSizeY;

            //            if (string.IsNullOrEmpty(ProgramConstants.GAME_NAME_SHORT))
            //                throw new ClientConfigurationException("LocalGame is set to an empty value.");

            //            if (ProgramConstants.GAME_NAME_SHORT.Length > ProgramConstants.GAME_ID_MAX_LENGTH)
            //            {
            //                throw new ClientConfigurationException("LocalGame is set to a value that exceeds length limit of " +
            //                    ProgramConstants.GAME_ID_MAX_LENGTH + " characters.");
            //            }

            //            logger.LogInformation("***Logfile for " + ProgramConstants.GAME_NAME_LONG + " client***");
            //            logger.LogInformation("Client version: " + Assembly.GetAssembly(typeof(PreStartup)).GetName().Version);

            //            // Log information about given startup params
            //            if (parameters.NoAudio)
            //            {
            //                logger.LogInformation("Startup parameter: No audio");

            //                // TODO fix
            //                throw new NotImplementedException("-NOAUDIO is currently not implemented, please run the client without it.".L10N("UI:Main:NoAudio"));
            //            }

            //            if (parameters.MultipleInstanceMode)
            //                logger.LogInformation("Startup parameter: Allow multiple client instances");

            //            parameters.UnknownStartupParams.ForEach(p => logger.LogWarning("Unknown startup parameter: " + p));

            //            logger.LogInformation("Loading settings.");

            //            // Try to load translations
            //            try
            //            {
            //                TranslationTable translation;
            //                var iniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, ClientConfiguration.Instance.TranslationIniName);

            //                if (iniFileInfo.Exists)
            //                {
            //                    translation = TranslationTable.LoadFromIniFile(iniFileInfo.FullName);
            //                }
            //                else
            //                {
            //                    logger.LogWarning("Failed to load the translation file. File does not exist.");

            //                    translation = new TranslationTable();
            //                }

            //                TranslationTable.Instance = translation;
            //                logger.LogInformation("Load translation: " + translation.LanguageName);
            //            }
            //            catch (Exception ex)
            //            {
            //                logger.LogExceptionDetails(ex, "Failed to load the translation file.");
            //                TranslationTable.Instance = new TranslationTable();
            //            }

            //            try
            //            {
            //                if (ClientConfiguration.Instance.GenerateTranslationStub)
            //                {
            //                    string stubPath = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "Translation.stub.ini");
            //                    var stubTable = TranslationTable.Instance.Clone();
            //                    TranslationTable.Instance.MissingTranslationEvent += (sender, e) =>
            //                    {
            //                        stubTable.Table.Add(e.Label, e.DefaultValue);
            //                    };

            //                    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            //                    {
            //                        logger.LogInformation("Writing the translation stub file.");
            //                        var ini = stubTable.SaveIni();
            //                        ini.WriteIniFile(stubPath);
            //                    };

            //                    logger.LogInformation("Generating translation stub feature is now enabled. The stub file will be written when the client exits.");
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                logger.LogExceptionDetails(ex, "Failed to generate the translation stub.");
            //            }

            //            // Delete obsolete files from old target project versions

            //            gameDirectory.EnumerateFiles("mainclient.log").SingleOrDefault()?.Delete();
            //            gameDirectory.EnumerateFiles("aunchupdt.dat").SingleOrDefault()?.Delete();

            //            try
            //            {
            //                gameDirectory.EnumerateFiles("wsock32.dll").SingleOrDefault()?.Delete();
            //            }
            //            catch (Exception ex)
            //            {
            //                logger.LogExceptionDetails(ex);

            //                string error = "Deleting wsock32.dll failed! Please close any " +
            //                    "applications that could be using the file, and then start the client again."
            //                    + Environment.NewLine + Environment.NewLine +
            //                    "Message: " + ex.Message;

            //                errorHandler.DisplayErrorAction(null, error, true);
            //            }

            //#if WINFORMS
            //            ApplicationConfiguration.Initialize();
            //#endif
            //            serviceProvider.GetService<Startup>().Execute();
        }

        public static void PostInitialize()
        {
            LoadingScreen loadingScreen = serviceProvider.GetService<LoadingScreen>();
            WindowManager windowManager = serviceProvider.GetService<WindowManager>();
            windowManager.AddAndInitializeControl(loadingScreen);
            loadingScreen.ClientRectangle = new Rectangle((windowManager.RenderResolutionX - loadingScreen.Width) / 2,
                (windowManager.RenderResolutionY - loadingScreen.Height) / 2, loadingScreen.Width, loadingScreen.Height);

            GameCollection gameCollection = serviceProvider.GetService<GameCollection>();

            gameCollection.Initialize();
            DirectoryInfo clientUserFilesDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);
            if (!clientUserFilesDirectory.Exists)
                clientUserFilesDirectory.Create();

            FileInfo clientLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client.log");
            clientLogFile.Delete();

            ProgramConstants.OSId = ClientConfiguration.Instance.GetOperatingSystemVersion();
            ProgramConstants.GAME_NAME_SHORT = ClientConfiguration.Instance.LocalGame;
            ProgramConstants.GAME_NAME_LONG = ClientConfiguration.Instance.LongGameName;
            ProgramConstants.SUPPORT_URL_SHORT = ClientConfiguration.Instance.ShortSupportURL;
            ProgramConstants.CREDITS_URL = ClientConfiguration.Instance.CreditsURL;
            ProgramConstants.MAP_CELL_SIZE_X = ClientConfiguration.Instance.MapCellSizeX;
            ProgramConstants.MAP_CELL_SIZE_Y = ClientConfiguration.Instance.MapCellSizeY;

            if (string.IsNullOrEmpty(ProgramConstants.GAME_NAME_SHORT))
                throw new ClientConfigurationException("LocalGame is set to an empty value.");

            if (ProgramConstants.GAME_NAME_SHORT.Length > ProgramConstants.GAME_ID_MAX_LENGTH)
            {
                throw new ClientConfigurationException("LocalGame is set to a value that exceeds length limit of " +
                    ProgramConstants.GAME_ID_MAX_LENGTH + " characters.");
            }

            logger.LogInformation("***Logfile for " + ProgramConstants.GAME_NAME_LONG + " client***");
            logger.LogInformation("Client version: " + Assembly.GetAssembly(typeof(PreStartup)).GetName().Version);

            // Log information about given startup params
            if (startupParams.NoAudio)
            {
                logger.LogInformation("Startup parameter: No audio");

                // TODO fix
                throw new NotImplementedException("-NOAUDIO is currently not implemented, please run the client without it.".L10N("UI:Main:NoAudio"));
            }

            if (startupParams.MultipleInstanceMode)
                logger.LogInformation("Startup parameter: Allow multiple client instances");

            startupParams.UnknownStartupParams.ForEach(p => logger.LogWarning("Unknown startup parameter: " + p));

            logger.LogInformation("Loading settings.");

            // Try to load translations
            try
            {
                TranslationTable translation;
                var iniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, ClientConfiguration.Instance.TranslationIniName);

                if (iniFileInfo.Exists)
                {
                    translation = TranslationTable.LoadFromIniFile(iniFileInfo.FullName);
                }
                else
                {
                    logger.LogWarning("Failed to load the translation file. File does not exist.");

                    translation = new TranslationTable();
                }

                TranslationTable.Instance = translation;
                logger.LogInformation("Load translation: " + translation.LanguageName);
            }
            catch (Exception ex)
            {
                logger.LogExceptionDetails(ex, "Failed to load the translation file.");
                TranslationTable.Instance = new TranslationTable();
            }

            try
            {
                if (ClientConfiguration.Instance.GenerateTranslationStub)
                {
                    string stubPath = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "Translation.stub.ini");
                    var stubTable = TranslationTable.Instance.Clone();
                    TranslationTable.Instance.MissingTranslationEvent += (sender, e) =>
                    {
                        stubTable.Table.Add(e.Label, e.DefaultValue);
                    };

                    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                    {
                        logger.LogInformation("Writing the translation stub file.");
                        var ini = stubTable.SaveIni();
                        ini.WriteIniFile(stubPath);
                    };

                    logger.LogInformation("Generating translation stub feature is now enabled. The stub file will be written when the client exits.");
                }
            }
            catch (Exception ex)
            {
                logger.LogExceptionDetails(ex, "Failed to generate the translation stub.");
            }

            // Delete obsolete files from old target project versions
            DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);

            gameDirectory.EnumerateFiles("mainclient.log").SingleOrDefault()?.Delete();
            gameDirectory.EnumerateFiles("aunchupdt.dat").SingleOrDefault()?.Delete();
            ErrorHandler errorHandler = serviceProvider.GetService<ErrorHandler>();

            try
            {
                gameDirectory.EnumerateFiles("wsock32.dll").SingleOrDefault()?.Delete();
            }
            catch (Exception ex)
            {
                logger.LogExceptionDetails(ex);

                string error = "Deleting wsock32.dll failed! Please close any " +
                    "applications that could be using the file, and then start the client again."
                    + Environment.NewLine + Environment.NewLine +
                    "Message: " + ex.Message;

                errorHandler.DisplayErrorAction(null, error, true);
            }

#if WINFORMS
            ApplicationConfiguration.Initialize();
#endif
            serviceProvider.GetService<Startup>().Execute();
        }

        [SupportedOSPlatform("windows")]
        private static void CheckPermissions(ErrorHandler errorHandler)
        {
            if (UserHasDirectoryAccessRights(ProgramConstants.GamePath, FileSystemRights.Modify))
                return;

            string error = string.Format(("You seem to be running {0} from a write-protected directory." + Environment.NewLine + Environment.NewLine +
                "For {1} to function properly when run from a write-protected directory, it needs administrative priveleges." + Environment.NewLine + Environment.NewLine +
                "Would you like to restart the client with administrative rights?" + Environment.NewLine + Environment.NewLine +
                "Please also make sure that your security software isn't blocking {1}.").L10N("UI:Main:AdminRequiredText"), ProgramConstants.GAME_NAME_LONG, ProgramConstants.GAME_NAME_SHORT);

            errorHandler.DisplayErrorAction("Administrative privileges required".L10N("UI:Main:AdminRequiredTitle"), error, false);

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = SafePath.CombineFilePath(ProgramConstants.StartupExecutable),
                Verb = "runas",
                CreateNoWindow = true
            });
            Environment.Exit(1);
        }

        /// <summary>
        /// Checks whether the client has specific file system rights to a directory.
        /// See ssds's answer at https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <param name="accessRights">The file system rights.</param>
        [SupportedOSPlatform("windows")]
        private static bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights)
        {
            var currentUser = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(currentUser);

            // If the user is not running the client with administrator privileges in Program Files, they need to be prompted to do so.
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                string progfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (ProgramConstants.GamePath.Contains(progfiles) || ProgramConstants.GamePath.Contains(progfilesx86))
                    return false;
            }

            var isInRoleWithAccess = false;

            try
            {
                var di = new DirectoryInfo(path);
                var acl = di.GetAccessControl();
                var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

                foreach (AuthorizationRule rule in rules)
                {
                    var fsAccessRule = rule as FileSystemAccessRule;
                    if (fsAccessRule == null)
                        continue;

                    if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                    {
                        var ntAccount = rule.IdentityReference as NTAccount;
                        if (ntAccount == null)
                            continue;

                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            return isInRoleWithAccess;
        }

        private static IServiceProvider BuildStartupServiceProvider()
        {
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services
                            .AddSingleton<GameClass>()
                            .AddSingleton<UserINISettings>()
                            .AddSingleton<ILogger, ClientCore.Extensions.Logger>();
                })
                .Build();

            return host.Services;
        }

        private static IServiceProvider BuildServiceProvider(IServiceProvider serviceProvider)
        {
            GameClass gameClass = serviceProvider.GetService<GameClass>();
            UserINISettings userINISettings = serviceProvider.GetService<UserINISettings>();
            ILogger logger = serviceProvider.GetService<ILogger>();
            (WindowManager windowManager, GraphicsDeviceManager graphicsDeviceManager) = gameClass.SetupDependencies();

            // Create host - this allows for things like DependencyInjection
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    // services (or service-like)
                    services
                            .AddSingleton<ErrorHandler>()
                            .AddSingleton(userINISettings)
                            .AddSingleton(logger)
                            .AddSingleton(gameClass)
                            .AddSingleton(windowManager)
                            .AddSingleton(graphicsDeviceManager)
                            .AddSingleton<CampaignSelector>()
                            .AddSingleton<CnCNetPlayerCountTask>()
                            .AddSingleton<MapSharer>()
                            .AddSingleton<GameProcessLogic>()
                            .AddSingleton<StatisticsManager>()
                            .AddSingleton<MapCodeHelper>()
                            .AddSingleton<MapPreviewExtractor>()
                            .AddSingleton<UPnPHandler>()
                            .AddSingleton<PreprocessorBackgroundTask>()
                            .AddSingleton<Startup>()
                            .AddSingleton<GraphicsDevice>()
                            .AddSingleton<GameCollection>()
                            .AddSingleton<CnCNetUserData>()
                            .AddSingleton<CnCNetManager>()
                            .AddSingleton<TunnelHandler>()
                            .AddSingleton<LogFileStatisticsParser>()
                            .AddSingleton<DiscordHandler>()
                            .AddSingleton<PrivateMessageHandler>()
                            .AddSingleton<GameOptionPresets>()
                            .AddSingleton<CnCNetGameCheck>()
                            .AddSingleton<FileHashCalculator>()
                            .AddSingleton<Connection>()
                            .AddSingleton<MapLoader>()
                            .AddSingleton<SavedGameManager>();

                    // singleton xna controls - same instance on each request
                    services
                            .AddSingleton<LoadingScreen>()
                            .AddSingleton<HotkeyConfigurationWindow>()
                            .AddSingleton<TopBar>()
                            .AddSingleton<PasswordRequestWindow>()
                            .AddSingleton<CnCNetLoginWindow>()
                            .AddSingleton<LANGameLoadingLobby>()
                            .AddSingleton<LANGameLobby>()
                            .AddSingleton<GameCreationWindow>()
                            .AddSingleton<GameFiltersPanel>()
                            .AddSingleton<GameLoadingWindow>()
                            .AddSingleton<OptionsWindow>()
                            .AddSingleton<PrivateMessagingWindow>()
                            .AddSingleton<PrivateMessagingPanel>()
                            .AddSingleton<LANLobby>()
                            .AddSingleton<CnCNetGameLobby>()
                            .AddSingleton<CnCNetGameLoadingLobby>()
                            .AddSingleton<CnCNetLobby>()
                            .AddSingleton<GameInProgressWindow>()
                            .AddSingleton<SkirmishLobby>()
                            .AddSingleton<MainMenu>()
                            .AddSingleton<CheaterWindow>()
                            .AddTransient<GameOptionsPanel>()
                            .AddTransient<AudioOptionsPanel>()
                            .AddTransient<DisplayOptionsPanel>()
                            .AddTransient<UpdaterOptionsPanel>()
                            .AddTransient<MainMenuDarkeningPanel>()
                            .AddTransient<ExtrasWindow>()
                            .AddTransient<UpdateWindow>()
                            .AddTransient<TunnelSelectionWindow>()
                            .AddTransient<UpdateQueryWindow>()
                            .AddTransient<PrivacyNotification>()
                            .AddTransient<ManualUpdateQueryWindow>()
                            .AddTransient<StatisticsWindow>()
                            .AddTransient<LANGameCreationWindow>();

                    // transient xna controls - new instance on each request
                    services
                            .AddTransient<CnCNetOptionsPanel>()
                            .AddTransient<GameListBox>()
                            .AddTransient<GlobalContextMenu>()
                            .AddTransient<GameLaunchButton>()
                            .AddTransient<LoadOrSaveGameOptionPresetWindow>()
                            .AddTransient<PlayerExtraOptionsPanel>()
                            .AddTransient<MapPreviewBox>()
                            .AddTransient<ComponentsPanel>()
                            .AddTransient<XNAMessageBox>()
                            .AddTransient<XNAWindow>()
                            .AddTransient<XNAControl>()
                            .AddTransient<XNAButton>()
                            .AddTransient<XNAClientButton>()
                            .AddTransient<XNAClientCheckBox>()
                            .AddTransient<XNAClientDropDown>()
                            .AddTransient<XNALinkButton>()
                            .AddTransient<XNAExtraPanel>()
                            .AddTransient<XNACheckBox>()
                            .AddTransient<XNADropDown>()
                            .AddTransient<XNALabel>()
                            .AddTransient<XNALinkLabel>()
                            .AddTransient<XNAListBox>()
                            .AddTransient<XNAMultiColumnListBox>()
                            .AddTransient<XNAPanel>()
                            .AddTransient<XNAProgressBar>()
                            .AddTransient<XNASuggestionTextBox>()
                            .AddTransient<XNATextBox>()
                            .AddTransient<XNATrackbar>()
                            .AddTransient<XNAChatTextBox>()
                            .AddTransient<ChatListBox>()
                            .AddTransient<GameLobbyCheckBox>()
                            .AddTransient<GameLobbyDropDown>()
                            .AddTransient<SettingCheckBox>();
                })
                .Build();

            return host.Services;
        }
    }
}