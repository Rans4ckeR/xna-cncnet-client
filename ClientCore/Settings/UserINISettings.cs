using System;
using System.Collections.Generic;
using ClientCore.Enums;
using ClientCore.Settings;
using Microsoft.Extensions.Logging;
using Rampastring.Tools;

namespace ClientCore
{
    public sealed class UserINISettings
    {
        public const string VIDEO = "Video";
        public const string MULTIPLAYER = "MultiPlayer";
        public const string OPTIONS = "Options";
        public const string AUDIO = "Audio";
        public const string COMPATIBILITY = "Compatibility";
        public const string GAME_FILTERS = "GameFilters";

        private const bool DEFAULT_SHOW_FRIENDS_ONLY_GAMES = false;
        private const bool DEFAULT_HIDE_LOCKED_GAMES = false;
        private const bool DEFAULT_HIDE_PASSWORDED_GAMES = false;
        private const bool DEFAULT_HIDE_INCOMPATIBLE_GAMES = false;
        private const int DEFAULT_MAX_PLAYER_COUNT = 8;

        private readonly ILogger logger;

        public UserINISettings(ILogger logger)
        {
            this.logger = logger;
            SettingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.SettingsIniName));

            const string WINDOWED_MODE_KEY = "Video.Windowed";
#if TS
            BackBufferInVRAM = new BoolSetting(SettingsIni, VIDEO, "UseGraphicsPatch", true);
#else
            BackBufferInVRAM = new BoolSetting(SettingsIni, VIDEO, "VideoBackBuffer", false);
#endif

            IngameScreenWidth = new IntSetting(SettingsIni, VIDEO, "ScreenWidth", 1024);
            IngameScreenHeight = new IntSetting(SettingsIni, VIDEO, "ScreenHeight", 768);
            ClientTheme = new StringSetting(SettingsIni, MULTIPLAYER, "Theme", string.Empty);
            DetailLevel = new IntSetting(SettingsIni, OPTIONS, "DetailLevel", 2);
            Renderer = new StringSetting(SettingsIni, COMPATIBILITY, "Renderer", string.Empty);
            WindowedMode = new BoolSetting(SettingsIni, VIDEO, WINDOWED_MODE_KEY, false);
            BorderlessWindowedMode = new BoolSetting(SettingsIni, VIDEO, "NoWindowFrame", false);
            BorderlessWindowedClient = new BoolSetting(SettingsIni, VIDEO, "BorderlessWindowedClient", true);
            ClientFPS = new IntSetting(SettingsIni, VIDEO, "ClientFPS", 60);
            DisplayToggleableExtraTextures = new BoolSetting(SettingsIni, VIDEO, "DisplayToggleableExtraTextures", true);

            ScoreVolume = new DoubleSetting(SettingsIni, AUDIO, "ScoreVolume", 0.7);
            SoundVolume = new DoubleSetting(SettingsIni, AUDIO, "SoundVolume", 0.7);
            VoiceVolume = new DoubleSetting(SettingsIni, AUDIO, "VoiceVolume", 0.7);
            IsScoreShuffle = new BoolSetting(SettingsIni, AUDIO, "IsScoreShuffle", true);
            ClientVolume = new DoubleSetting(SettingsIni, AUDIO, "ClientVolume", 1.0);
            PlayMainMenuMusic = new BoolSetting(SettingsIni, AUDIO, "PlayMainMenuMusic", true);
            StopMusicOnMenu = new BoolSetting(SettingsIni, AUDIO, "StopMusicOnMenu", true);
            MessageSound = new BoolSetting(SettingsIni, AUDIO, "ChatMessageSound", true);

            ScrollRate = new IntSetting(SettingsIni, OPTIONS, "ScrollRate", 3);
            DragDistance = new IntSetting(SettingsIni, OPTIONS, "DragDistance", 4);
            DoubleTapInterval = new IntSetting(SettingsIni, OPTIONS, "DoubleTapInterval", 30);
            Win8CompatMode = new StringSetting(SettingsIni, OPTIONS, "Win8Compat", "No");

            PlayerName = new StringSetting(SettingsIni, MULTIPLAYER, "Handle", string.Empty);

            ChatColor = new IntSetting(SettingsIni, MULTIPLAYER, "ChatColor", -1);
            LANChatColor = new IntSetting(SettingsIni, MULTIPLAYER, "LANChatColor", -1);
            PingUnofficialCnCNetTunnels = new BoolSetting(SettingsIni, MULTIPLAYER, "PingCustomTunnels", true);
            WritePathToRegistry = new BoolSetting(SettingsIni, OPTIONS, "WriteInstallationPathToRegistry", true);
            PlaySoundOnGameHosted = new BoolSetting(SettingsIni, MULTIPLAYER, "PlaySoundOnGameHosted", true);
            SkipConnectDialog = new BoolSetting(SettingsIni, MULTIPLAYER, "SkipConnectDialog", false);
            PersistentMode = new BoolSetting(SettingsIni, MULTIPLAYER, "PersistentMode", false);
            AutomaticCnCNetLogin = new BoolSetting(SettingsIni, MULTIPLAYER, "AutomaticCnCNetLogin", false);
            DiscordIntegration = new BoolSetting(SettingsIni, MULTIPLAYER, "DiscordIntegration", true);
            AllowGameInvitesFromFriendsOnly = new BoolSetting(SettingsIni, MULTIPLAYER, "AllowGameInvitesFromFriendsOnly", false);
            NotifyOnUserListChange = new BoolSetting(SettingsIni, MULTIPLAYER, "NotifyOnUserListChange", true);
            DisablePrivateMessagePopups = new BoolSetting(SettingsIni, MULTIPLAYER, "DisablePrivateMessagePopups", false);
            AllowPrivateMessagesFromState = new IntSetting(SettingsIni, MULTIPLAYER, "AllowPrivateMessagesFromState", (int)AllowPrivateMessagesFromEnum.All);
            EnableMapSharing = new BoolSetting(SettingsIni, MULTIPLAYER, "EnableMapSharing", true);
            AlwaysDisplayTunnelList = new BoolSetting(SettingsIni, MULTIPLAYER, "AlwaysDisplayTunnelList", false);
            MapSortState = new IntSetting(SettingsIni, MULTIPLAYER, "MapSortState", (int)SortDirection.None);
            UseLegacyTunnels = new BoolSetting(SettingsIni, MULTIPLAYER, "UseLegacyTunnels", false);
            UseP2P = new BoolSetting(SettingsIni, MULTIPLAYER, "UseP2P", false);
            UseDynamicTunnels = new BoolSetting(SettingsIni, MULTIPLAYER, "UseDynamicTunnels", true);

            CheckForUpdates = new BoolSetting(SettingsIni, OPTIONS, "CheckforUpdates", true);

            PrivacyPolicyAccepted = new BoolSetting(SettingsIni, OPTIONS, "PrivacyPolicyAccepted", false);
            IsFirstRun = new BoolSetting(SettingsIni, OPTIONS, "IsFirstRun", true);
            CustomComponentsDenied = new BoolSetting(SettingsIni, OPTIONS, "CustomComponentsDenied", false);
            Difficulty = new IntSetting(SettingsIni, OPTIONS, "Difficulty", 1);
            ScrollDelay = new IntSetting(SettingsIni, OPTIONS, "ScrollDelay", 4);
            GameSpeed = new IntSetting(SettingsIni, OPTIONS, "GameSpeed", 1);
            PreloadMapPreviews = new BoolSetting(SettingsIni, VIDEO, "PreloadMapPreviews", false);
            ForceLowestDetailLevel = new BoolSetting(SettingsIni, VIDEO, "ForceLowestDetailLevel", false);
            MinimizeWindowsOnGameStart = new BoolSetting(SettingsIni, OPTIONS, "MinimizeWindowsOnGameStart", true);
            AutoRemoveUnderscoresFromName = new BoolSetting(SettingsIni, OPTIONS, "AutoRemoveUnderscoresFromName", true);

            SortState = new IntSetting(SettingsIni, GAME_FILTERS, "SortState", (int)SortDirection.None);
            ShowFriendGamesOnly = new BoolSetting(SettingsIni, GAME_FILTERS, "ShowFriendGamesOnly", DEFAULT_SHOW_FRIENDS_ONLY_GAMES);
            HideLockedGames = new BoolSetting(SettingsIni, GAME_FILTERS, "HideLockedGames", DEFAULT_HIDE_LOCKED_GAMES);
            HidePasswordedGames = new BoolSetting(SettingsIni, GAME_FILTERS, "HidePasswordedGames", DEFAULT_HIDE_PASSWORDED_GAMES);
            HideIncompatibleGames = new BoolSetting(SettingsIni, GAME_FILTERS, "HideIncompatibleGames", DEFAULT_HIDE_INCOMPATIBLE_GAMES);
            MaxPlayerCount = new IntRangeSetting(SettingsIni, GAME_FILTERS, "MaxPlayerCount", DEFAULT_MAX_PLAYER_COUNT, 2, 8);

            FavoriteMaps = new StringListSetting(SettingsIni, OPTIONS, "FavoriteMaps", new List<string>());
        }

        public IniFile SettingsIni { get; private set; }

        public event EventHandler SettingsSaved;

        /*********/
        /* VIDEO */
        /*********/

        public IntSetting IngameScreenWidth { get; private set; }

        public IntSetting IngameScreenHeight { get; private set; }

        public StringSetting ClientTheme { get; private set; }

        public IntSetting DetailLevel { get; private set; }

        public StringSetting Renderer { get; private set; }

        public BoolSetting WindowedMode { get; private set; }

        public BoolSetting BorderlessWindowedMode { get; private set; }

        public BoolSetting BackBufferInVRAM { get; private set; }

        public IntSetting ClientResolutionX { get; set; }

        public IntSetting ClientResolutionY { get; set; }

        public BoolSetting BorderlessWindowedClient { get; private set; }

        public IntSetting ClientFPS { get; private set; }

        public BoolSetting DisplayToggleableExtraTextures { get; private set; }

        /*********/
        /* AUDIO */
        /*********/

        public DoubleSetting ScoreVolume { get; private set; }

        public DoubleSetting SoundVolume { get; private set; }

        public DoubleSetting VoiceVolume { get; private set; }

        public BoolSetting IsScoreShuffle { get; private set; }

        public DoubleSetting ClientVolume { get; private set; }

        public BoolSetting PlayMainMenuMusic { get; private set; }

        public BoolSetting StopMusicOnMenu { get; private set; }

        public BoolSetting MessageSound { get; private set; }

        /********/
        /* GAME */
        /********/

        public IntSetting ScrollRate { get; private set; }

        public IntSetting DragDistance { get; private set; }

        public IntSetting DoubleTapInterval { get; private set; }

        public StringSetting Win8CompatMode { get; private set; }

        /************************/
        /* MULTIPLAYER (CnCNet) */
        /************************/

        public StringSetting PlayerName { get; private set; }

        public IntSetting ChatColor { get; private set; }

        public IntSetting LANChatColor { get; private set; }

        public BoolSetting PingUnofficialCnCNetTunnels { get; private set; }

        public BoolSetting WritePathToRegistry { get; private set; }

        public BoolSetting PlaySoundOnGameHosted { get; private set; }

        public BoolSetting SkipConnectDialog { get; private set; }

        public BoolSetting PersistentMode { get; private set; }

        public BoolSetting AutomaticCnCNetLogin { get; private set; }

        public BoolSetting DiscordIntegration { get; private set; }

        public BoolSetting AllowGameInvitesFromFriendsOnly { get; private set; }

        public BoolSetting NotifyOnUserListChange { get; private set; }

        public BoolSetting DisablePrivateMessagePopups { get; private set; }

        public IntSetting AllowPrivateMessagesFromState { get; private set; }

        public BoolSetting EnableMapSharing { get; private set; }

        public BoolSetting AlwaysDisplayTunnelList { get; private set; }

        public IntSetting MapSortState { get; private set; }

        public BoolSetting UseLegacyTunnels { get; private set; }

        public BoolSetting UseP2P { get; private set; }

        public BoolSetting UseDynamicTunnels { get; private set; }

        /*********************/
        /* GAME LIST FILTERS */
        /*********************/

        public IntSetting SortState { get; private set; }

        public BoolSetting ShowFriendGamesOnly { get; private set; }

        public BoolSetting HideLockedGames { get; private set; }

        public BoolSetting HidePasswordedGames { get; private set; }

        public BoolSetting HideIncompatibleGames { get; private set; }

        public IntRangeSetting MaxPlayerCount { get; private set; }

        /********/
        /* MISC */
        /********/

        public BoolSetting CheckForUpdates { get; private set; }

        public BoolSetting PrivacyPolicyAccepted { get; private set; }

        public BoolSetting IsFirstRun { get; private set; }

        public BoolSetting CustomComponentsDenied { get; private set; }

        public IntSetting Difficulty { get; private set; }

        public IntSetting GameSpeed { get; private set; }

        public IntSetting ScrollDelay { get; private set; }

        public BoolSetting PreloadMapPreviews { get; private set; }

        public BoolSetting ForceLowestDetailLevel { get; private set; }

        public BoolSetting MinimizeWindowsOnGameStart { get; private set; }

        public BoolSetting AutoRemoveUnderscoresFromName { get; private set; }

        public StringListSetting FavoriteMaps { get; private set; }

        public void SetValue(string section, string key, string value)
               => SettingsIni.SetStringValue(section, key, value);

        public void SetValue(string section, string key, bool value)
            => SettingsIni.SetBooleanValue(section, key, value);

        public void SetValue(string section, string key, int value)
            => SettingsIni.SetIntValue(section, key, value);

        public string GetValue(string section, string key, string defaultValue)
            => SettingsIni.GetStringValue(section, key, defaultValue);

        public bool GetValue(string section, string key, bool defaultValue)
            => SettingsIni.GetBooleanValue(section, key, defaultValue);

        public int GetValue(string section, string key, int defaultValue)
            => SettingsIni.GetIntValue(section, key, defaultValue);

        public bool IsGameFollowed(string gameName)
        {
            return SettingsIni.GetBooleanValue("Channels", gameName, false);
        }

        public bool ToggleFavoriteMap(string mapName, string gameModeName, bool isFavorite)
        {
            if (string.IsNullOrEmpty(mapName))
                return isFavorite;

            var favoriteMapKey = FavoriteMapKey(mapName, gameModeName);
            isFavorite = IsFavoriteMap(mapName, gameModeName);
            if (isFavorite)
                FavoriteMaps.Remove(favoriteMapKey);
            else
                FavoriteMaps.Add(favoriteMapKey);

            SaveSettings();

            return !isFavorite;
        }

        /// <summary>
        /// Checks if a specified map name and game mode name belongs to the favorite map list.
        /// </summary>
        /// <param name="nameName">The name of the map.</param>
        /// <param name="gameModeName">The name of the game mode</param>
        public bool IsFavoriteMap(string nameName, string gameModeName) => FavoriteMaps.Value.Contains(FavoriteMapKey(nameName, gameModeName));

        private string FavoriteMapKey(string nameName, string gameModeName) => $"{nameName}:{gameModeName}";

        public void ReloadSettings()
        {
            SettingsIni.Reload();
        }

        public void ApplyDefaults()
        {
            ForceLowestDetailLevel.SetDefaultIfNonexistent();
            DoubleTapInterval.SetDefaultIfNonexistent();
            ScrollDelay.SetDefaultIfNonexistent();
        }

        public void SaveSettings()
        {
            logger.LogInformation("Writing settings INI.");

            ApplyDefaults();

            SettingsIni.WriteIniFile();

            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }

        public bool IsGameFiltersApplied()
        {
            return ShowFriendGamesOnly.Value != DEFAULT_SHOW_FRIENDS_ONLY_GAMES ||
                   HideLockedGames.Value != DEFAULT_HIDE_LOCKED_GAMES ||
                   HidePasswordedGames.Value != DEFAULT_HIDE_PASSWORDED_GAMES ||
                   HideIncompatibleGames.Value != DEFAULT_HIDE_INCOMPATIBLE_GAMES ||
                   MaxPlayerCount.Value != DEFAULT_MAX_PLAYER_COUNT;
        }

        public void ResetGameFilters()
        {
            ShowFriendGamesOnly.Value = DEFAULT_SHOW_FRIENDS_ONLY_GAMES;
            HideLockedGames.Value = DEFAULT_HIDE_LOCKED_GAMES;
            HideIncompatibleGames.Value = DEFAULT_HIDE_INCOMPATIBLE_GAMES;
            HidePasswordedGames.Value = DEFAULT_HIDE_PASSWORDED_GAMES;
            MaxPlayerCount.Value = DEFAULT_MAX_PLAYER_COUNT;
        }
    }
}