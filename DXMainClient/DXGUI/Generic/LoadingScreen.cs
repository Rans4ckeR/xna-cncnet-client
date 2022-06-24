using System.Threading.Tasks;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using ClientUpdater;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using DTAConfig;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using SkirmishLobby = DTAClient.DXGUI.Multiplayer.GameLobby.SkirmishLobby;

namespace DTAClient.DXGUI.Generic;

public class LoadingScreen : XNAWindow
{
    private static readonly object Locker = new();

    public LoadingScreen(WindowManager windowManager)
        : base(windowManager)
    {
    }

    private MapLoader mapLoader;

    private PrivateMessagingPanel privateMessagingPanel;

    private bool visibleSpriteCursor = false;

    private Task updaterInitTask = null;
    private Task mapLoadTask = null;

    public override void Initialize()
    {
        ClientRectangle = new Rectangle(0, 0, 800, 600);
        Name = "LoadingScreen";

        BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

        base.Initialize();

        CenterOnParent();

        bool initUpdater = !ClientConfiguration.Instance.ModMode;

        if (initUpdater)
        {
            updaterInitTask = new Task(InitUpdater);
            updaterInitTask.Start();
        }

        mapLoadTask = new Task(LoadMaps);
        mapLoadTask.Start();

        if (Cursor.Visible)
        {
            Cursor.Visible = false;
            visibleSpriteCursor = true;
        }
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

    private void InitUpdater()
    {
        Updater.OnLocalFileVersionsChecked += LogGameClientVersion;
        Updater.CheckLocalFileVersions();
    }

    private void LogGameClientVersion()
    {
        Logger.Log($"Game Client Version: {ClientConfiguration.Instance.LocalGame} {Updater.GameVersion}");
        Updater.OnLocalFileVersionsChecked -= LogGameClientVersion;
    }

    private void LoadMaps()
    {
        mapLoader = new MapLoader();
        mapLoader.LoadMaps();
    }

    private void Finish()
    {
        ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ?
            "N/A" : Updater.GameVersion;

        DiscordHandler discordHandler = null;
        if (!string.IsNullOrEmpty(ClientConfiguration.Instance.DiscordAppId))
            discordHandler = new DiscordHandler(WindowManager);

        ClientGUICreator.Instance.AddControl(typeof(GameLobbyCheckBox));
        ClientGUICreator.Instance.AddControl(typeof(GameLobbyDropDown));
        ClientGUICreator.Instance.AddControl(typeof(MapPreviewBox));
        ClientGUICreator.Instance.AddControl(typeof(GameLaunchButton));
        ClientGUICreator.Instance.AddControl(typeof(ChatListBox));
        ClientGUICreator.Instance.AddControl(typeof(XNAChatTextBox));
        ClientGUICreator.Instance.AddControl(typeof(PlayerExtraOptionsPanel));

        GameCollection gameCollection = new();
        gameCollection.Initialize(GraphicsDevice);

        LANLobby lanLobby = new(WindowManager, gameCollection, mapLoader.GameModes, mapLoader, discordHandler);

        CnCNetUserData cncnetUserData = new(WindowManager);
        CnCNetManager cncnetManager = new(WindowManager, gameCollection, cncnetUserData);
        TunnelHandler tunnelHandler = new(WindowManager, cncnetManager);
        PrivateMessageHandler privateMessageHandler = new(cncnetManager, cncnetUserData);

        TopBar topBar = new(WindowManager, cncnetManager, privateMessageHandler);

        OptionsWindow optionsWindow = new(WindowManager, gameCollection, topBar);

        PrivateMessagingWindow pmWindow = new(
            WindowManager,
            cncnetManager, gameCollection, cncnetUserData, privateMessageHandler);
        privateMessagingPanel = new PrivateMessagingPanel(WindowManager);

        CnCNetGameLobby cncnetGameLobby = new(
            WindowManager,
            "MultiplayerGameLobby", topBar, cncnetManager, tunnelHandler, gameCollection, cncnetUserData, mapLoader, discordHandler, pmWindow);
        CnCNetGameLoadingLobby cncnetGameLoadingLobby = new(
            WindowManager,
            topBar, cncnetManager, tunnelHandler, mapLoader.GameModes, gameCollection, discordHandler);
        CnCNetLobby cncnetLobby = new(WindowManager, cncnetManager,
            cncnetGameLobby, cncnetGameLoadingLobby, topBar, pmWindow, tunnelHandler,
            gameCollection, cncnetUserData, optionsWindow);
        GameInProgressWindow gipw = new(WindowManager);

        SkirmishLobby skirmishLobby = new(WindowManager, topBar, mapLoader, discordHandler);

        topBar.SetSecondarySwitch(cncnetLobby);

        MainMenu mainMenu = new(WindowManager, skirmishLobby, lanLobby,
            topBar, optionsWindow, cncnetLobby, cncnetManager, discordHandler);
        WindowManager.AddAndInitializeControl(mainMenu);

        DarkeningPanel.AddAndInitializeWithControl(WindowManager, skirmishLobby);

        DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetGameLoadingLobby);

        DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetGameLobby);

        DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetLobby);

        DarkeningPanel.AddAndInitializeWithControl(WindowManager, lanLobby);

        DarkeningPanel.AddAndInitializeWithControl(WindowManager, optionsWindow);

        WindowManager.AddAndInitializeControl(privateMessagingPanel);
        privateMessagingPanel.AddChild(pmWindow);

        topBar.SetTertiarySwitch(pmWindow);
        topBar.SetOptionsWindow(optionsWindow);

        WindowManager.AddAndInitializeControl(gipw);
        skirmishLobby.Disable();
        cncnetLobby.Disable();
        cncnetGameLobby.Disable();
        cncnetGameLoadingLobby.Disable();
        lanLobby.Disable();
        pmWindow.Disable();
        optionsWindow.Disable();

        WindowManager.AddAndInitializeControl(topBar);
        topBar.AddPrimarySwitchable(mainMenu);

        mainMenu.PostInit();

        if (UserINISettings.Instance.AutomaticCnCNetLogin &&
            NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
        {
            cncnetManager.Connect();
        }

        if (!UserINISettings.Instance.PrivacyPolicyAccepted)
        {
            WindowManager.AddAndInitializeControl(new PrivacyNotification(WindowManager));
        }

        WindowManager.RemoveControl(this);

        Cursor.Visible = visibleSpriteCursor;
    }

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
    }
}