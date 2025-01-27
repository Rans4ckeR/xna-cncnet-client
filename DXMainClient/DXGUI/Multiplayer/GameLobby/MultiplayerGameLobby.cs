﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using ClientCore;
using System.IO;
using Rampastring.Tools;
using ClientCore.Statistics;
using DTAClient.DXGUI.Generic;
using DTAClient.Domain.Multiplayer;
using ClientGUI;
using System.Text;
using System.Threading.Tasks;
using ClientCore.Extensions;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer.CnCNet;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    using System.Globalization;

    /// <summary>
    /// A generic base class for multiplayer game lobbies (CnCNet and LAN).
    /// </summary>
    internal abstract class MultiplayerGameLobby : GameLobbyBase, ISwitchable
    {
        private const int MAX_DICE = 10;
        private const int MAX_DIE_SIDES = 100;

        public MultiplayerGameLobby(
            WindowManager windowManager,
            string iniName,
            TopBar topBar,
            MapLoader mapLoader,
            DiscordHandler discordHandler)
            : base(windowManager, iniName, mapLoader, true, discordHandler)
        {
            TopBar = topBar;

            chatBoxCommands =
            [
                new(CnCNetLobbyCommands.HIDEMAPS, "Hide map list (game host only)".L10N("Client:Main:ChatboxCommandHideMapsHelp"), true,
                    s => HideMapList()),
                new(CnCNetLobbyCommands.SHOWMAPS, "Show map list (game host only)".L10N("Client:Main:ChatboxCommandShowMapsHelp"), true,
                    s => ShowMapList()),
                new(CnCNetLobbyCommands.FRAMESENDRATE, "Change order lag / FrameSendRate (default 7) (game host only)".L10N("Client:Main:ChatboxCommandFrameSendRateHelp"), true,
                    s => SetFrameSendRateAsync(s).HandleTask()),
                new(CnCNetLobbyCommands.MAXAHEAD, "Change MaxAhead (default 0) (game host only)".L10N("Client:Main:ChatboxCommandMaxAheadHelp"), true,
                    s => SetMaxAheadAsync(s).HandleTask()),
                new(CnCNetLobbyCommands.PROTOCOLVERSION, "Change ProtocolVersion (default 2) (game host only)".L10N("Client:Main:ChatboxCommandProtocolVersionHelp"), true,
                    s => SetProtocolVersionAsync(s).HandleTask()),
                new(CnCNetLobbyCommands.LOADMAP, "Load a custom map with given filename from /Maps/Custom/ folder.".L10N("Client:Main:ChatboxCommandLoadMapHelp"), true, LoadCustomMap),
                new(CnCNetLobbyCommands.RANDOMSTARTS, "Enables completely random starting locations (Tiberian Sun based games only).".L10N("Client:Main:ChatboxCommandRandomStartsHelp"), true,
                    s => SetStartingLocationClearanceAsync(s).HandleTask()),
                new(CnCNetLobbyCommands.ROLL, "Roll dice, for example /roll 3d6".L10N("Client:Main:ChatboxCommandRollHelp"), false, dieType => RollDiceCommandAsync(dieType).HandleTask()),
                new(CnCNetLobbyCommands.SAVEOPTIONS, "Save game option preset so it can be loaded later".L10N("Client:Main:ChatboxCommandSaveOptionsHelp"), false, HandleGameOptionPresetSaveCommand),
                new(CnCNetLobbyCommands.LOADOPTIONS, "Load game option preset".L10N("Client:Main:ChatboxCommandLoadOptionsHelp"), true, presetName => HandleGameOptionPresetLoadCommandAsync(presetName).HandleTask())
            ];

            chkAutoReady_CheckedChangedFunc = (_, _) => ChkAutoReady_CheckedChangedAsync().HandleTask();
        }

        protected XNAPlayerSlotIndicator[] StatusIndicators;

        protected ChatListBox lbChatMessages;
        protected XNAChatTextBox tbChatInput;
        protected XNAClientButton btnLockGame;
        protected XNAClientCheckBox chkAutoReady;

        protected bool IsHost = false;

        private bool locked = false;
        protected bool Locked
        {
            get => locked;
            set
            {
                bool oldLocked = locked;
                locked = value;
                if (oldLocked != value)
                {
                    CopyPlayerDataToUI();
                    UpdateDiscordPresence();
                }
            }
        }

        // protected bool DisableSpectatorReadyChecking = false;

        protected EnhancedSoundEffect sndJoinSound;
        protected EnhancedSoundEffect sndLeaveSound;
        protected EnhancedSoundEffect sndMessageSound;
        protected EnhancedSoundEffect sndGetReadySound;
        protected EnhancedSoundEffect sndReturnSound;

        protected Texture2D[] PingTextures;

        protected TopBar TopBar;

        protected int FrameSendRate { get; set; } = 7;

        /// <summary>
        /// Controls the MaxAhead parameter. The default value of 0 means that 
        /// the value is not written to spawn.ini, which allows the spawner the
        /// calculate and assign the MaxAhead value.
        /// </summary>
        protected int MaxAhead { get; set; }

        protected int ProtocolVersion { get; set; } = 2;

        protected List<ChatBoxCommand> chatBoxCommands;

        private FileSystemWatcher fsw;

        private bool gameSaved;

        private bool lastMapChangeWasInvalid;

        private EventHandler chkAutoReady_CheckedChangedFunc;

        /// <summary>
        /// Allows derived classes to add their own chat box commands.
        /// </summary>
        /// <param name="command">The command to add.</param>
        protected void AddChatBoxCommand(ChatBoxCommand command) => chatBoxCommands.Add(command);

        public override void Initialize()
        {
            Name = nameof(MultiplayerGameLobby);

            base.Initialize();

            // DisableSpectatorReadyChecking = GameOptionsIni.GetBooleanValue("General", "DisableSpectatorReadyChecking", false);
            PingTextures = new[]
            {
                AssetLoader.LoadTexture("ping0.png"),
                AssetLoader.LoadTexture("ping1.png"),
                AssetLoader.LoadTexture("ping2.png"),
                AssetLoader.LoadTexture("ping3.png"),
                AssetLoader.LoadTexture("ping4.png")
            };

            InitPlayerOptionDropdowns();

            StatusIndicators = new XNAPlayerSlotIndicator[MAX_PLAYER_COUNT];

            int statusIndicatorX = ConfigIni.GetIntValue(Name, "PlayerStatusIndicatorX", 0);
            int statusIndicatorY = ConfigIni.GetIntValue(Name, "PlayerStatusIndicatorY", 0);

            for (int i = 0; i < MAX_PLAYER_COUNT; i++)
            {
                var indicatorPlayerReady = new XNAPlayerSlotIndicator(WindowManager);
                indicatorPlayerReady.Name = "playerStatusIndicator" + i;
                indicatorPlayerReady.ClientRectangle = new Rectangle(statusIndicatorX, ddPlayerTeams[i].Y + statusIndicatorY, 0, 0);

                PlayerOptionsPanel.AddChild(indicatorPlayerReady);

                StatusIndicators[i] = indicatorPlayerReady;

                const string spectatorName = "Spectator";
                AddSideToDropDown(ddPlayerSides[i], spectatorName, spectatorName.L10N("Client:Sides:SpectatorSide"), AssetLoader.LoadTexture("spectatoricon.png"));
            }

            lbChatMessages = FindChild<ChatListBox>(nameof(lbChatMessages));

            tbChatInput = FindChild<XNAChatTextBox>(nameof(tbChatInput));
            tbChatInput.MaximumTextLength = 150;
            tbChatInput.EnterPressed += (_, _) => TbChatInput_EnterPressedAsync().HandleTask();

            btnLockGame = FindChild<XNAClientButton>(nameof(btnLockGame));
            btnLockGame.LeftClick += (_, _) => HandleLockGameButtonClickAsync().HandleTask();

            chkAutoReady = FindChild<XNAClientCheckBox>(nameof(chkAutoReady));
            chkAutoReady.CheckedChanged += chkAutoReady_CheckedChangedFunc;
            chkAutoReady.Disable();

            MapPreviewBox.LocalStartingLocationSelected += MapPreviewBox_LocalStartingLocationSelected;
            MapPreviewBox.StartingLocationApplied += (_, _) => MapPreviewBox_StartingLocationAppliedAsync().HandleTask();

            sndJoinSound = new EnhancedSoundEffect("joingame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyJoinCooldown);
            sndLeaveSound = new EnhancedSoundEffect("leavegame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyLeaveCooldown);
            sndMessageSound = new EnhancedSoundEffect("message.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundMessageCooldown);
            sndGetReadySound = new EnhancedSoundEffect("getready.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyGetReadyCooldown);
            sndReturnSound = new EnhancedSoundEffect("return.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyReturnCooldown);

            if (SavedGameManager.AreSavedGamesAvailable())
            {
                fsw = new FileSystemWatcher(SafePath.CombineDirectoryPath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAMES_DIRECTORY), "*.NET");
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Created;
                fsw.EnableRaisingEvents = false;
            }
            else
            {
                Logger.Log("MultiplayerGameLobby: Saved games are not available!");
            }

            ParseHostPlayerControls();
        }

        /// <summary>
        /// Reads INI for host/player variations of controls and restores it back.
        /// </summary>
        /// <remarks>
        /// Needed for translation notification mechanism to work correctly.
        /// </remarks>
        private void ParseHostPlayerControls()
        {
            string temp = lbChatMessages.Name;

            lbChatMessages.Name = "lbChatMessages_Host";
            ReadINIForControl(lbChatMessages);
            lbChatMessages.Name = "lbChatMessages_Player";
            ReadINIForControl(lbChatMessages);
            lbChatMessages.Name = temp;
            ReadINIForControl(lbChatMessages);

            temp = tbChatInput.Name;

            tbChatInput.Name = "tbChatInput_Host";
            ReadINIForControl(tbChatInput);
            tbChatInput.Name = "tbChatInput_Player";
            ReadINIForControl(tbChatInput);
            tbChatInput.Name = temp;
            ReadINIForControl(tbChatInput);
        }

        /// <summary>
        /// Performs initialization that is necessary after derived
        /// classes have performed their own initialization.
        /// </summary>
        protected void PostInitialize()
        {
            CenterOnParent();
            LoadDefaultGameModeMap();
        }

        private void fsw_Created(object sender, FileSystemEventArgs e)
            => AddCallback(() => FSWEventAsync(e).HandleTask());

        private ValueTask FSWEventAsync(FileSystemEventArgs e)
        {
            Logger.Log("FSW Event: " + e.FullPath);

            if (string.Equals(Path.GetFileName(e.FullPath), "SAVEGAME.NET", StringComparison.OrdinalIgnoreCase))
            {
                if (!gameSaved)
                {
                    bool success = SavedGameManager.InitSavedGames();

                    if (!success)
                        return default;
                }

                gameSaved = true;

                return SavedGameManager.RenameSavedGameAsync();
            }

            return default;
        }

        protected override ValueTask StartGameAsync()
        {
            if (fsw != null)
                fsw.EnableRaisingEvents = true;

            for (int pId = 0; pId < Players.Count; pId++)
                Players[pId].IsInGame = true;

            return base.StartGameAsync();
        }

        protected override async ValueTask GameProcessExitedAsync()
        {
            gameSaved = false;

            if (fsw != null)
                fsw.EnableRaisingEvents = false;

            PlayerInfo pInfo = Players.Find(p => string.Equals(p.Name, ProgramConstants.PLAYERNAME, StringComparison.OrdinalIgnoreCase));

            pInfo.IsInGame = false;

            await base.GameProcessExitedAsync().ConfigureAwait(true);

            if (IsHost)
            {
                GenerateGameID();
                await DdGameModeMapFilter_SelectedIndexChangedAsync().ConfigureAwait(false); // Refresh ranks
            }
            else if (chkAutoReady.Checked)
            {
                await RequestReadyStatusAsync().ConfigureAwait(false);
            }
        }

        private void GenerateGameID()
        {
            int i = 0;

            while (i < 20)
            {
                string s = DateTime.Now.Day +
                    DateTime.Now.Month +
                    DateTime.Now.Hour +
                    DateTime.Now.Minute.ToString(CultureInfo.InvariantCulture);

                UniqueGameID = int.Parse(i + s, CultureInfo.InvariantCulture);

                if (StatisticsManager.Instance.GetMatchWithGameID(UniqueGameID) == null)
                    break;

                i++;
            }
        }

        protected virtual ValueTask HandleLockGameButtonClickAsync()
        {
            if (Locked)
                return UnlockGameAsync(true);
            else
                return LockGameAsync();
        }

        protected abstract ValueTask LockGameAsync();

        protected abstract ValueTask UnlockGameAsync(bool announce);

        private async ValueTask TbChatInput_EnterPressedAsync()
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            if (tbChatInput.Text.StartsWith('/'))
            {
                string text = tbChatInput.Text;
                string command;
                string parameters;

                int spaceIndex = text.IndexOf(' ', StringComparison.OrdinalIgnoreCase);

                if (spaceIndex == -1)
                {
                    command = text[1..];
                    parameters = string.Empty;
                }
                else
                {
                    command = text[1..spaceIndex];
                    parameters = text[(spaceIndex + 1)..];
                }

                tbChatInput.Text = string.Empty;

                foreach (var chatBoxCommand in chatBoxCommands)
                {
                    if (string.Equals(command, chatBoxCommand.Command, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!IsHost && chatBoxCommand.HostOnly)
                        {
                            AddNotice(string.Format(CultureInfo.CurrentCulture, "/{0} is for game hosts only.".L10N("Client:Main:ChatboxCommandHostOnly"), chatBoxCommand.Command));
                            return;
                        }

                        chatBoxCommand.Action(parameters);
                        return;
                    }
                }

                StringBuilder sb = new StringBuilder("To use a command, start your message with /<command>. Possible chat box commands:".L10N("Client:Main:ChatboxCommandTipText") + " ");
                foreach (var chatBoxCommand in chatBoxCommands)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(Environment.NewLine);
#if NETFRAMEWORK
                    sb.Append($"{chatBoxCommand.Command}: {chatBoxCommand.Description}");
#else
                    sb.Append(CultureInfo.CurrentCulture, $"{chatBoxCommand.Command}: {chatBoxCommand.Description}");
#endif
                }
                XNAMessageBox.Show(WindowManager, "Chat Box Command Help".L10N("Client:Main:ChatboxCommandTipTitle"), sb.ToString());
                return;
            }

            await SendChatMessageAsync(tbChatInput.Text).ConfigureAwait(false);
            tbChatInput.Text = string.Empty;
        }

        private ValueTask ChkAutoReady_CheckedChangedAsync()
        {
            UpdateLaunchGameButtonStatus();
            return RequestReadyStatusAsync();
        }

        protected void ResetAutoReadyCheckbox()
        {
            chkAutoReady.CheckedChanged -= chkAutoReady_CheckedChangedFunc;
            chkAutoReady.Checked = false;
            chkAutoReady.CheckedChanged += chkAutoReady_CheckedChangedFunc;
            UpdateLaunchGameButtonStatus();
        }

        private async ValueTask SetFrameSendRateAsync(string value)
        {
            bool success = int.TryParse(value, out int intValue);

            if (!success)
            {
                AddNotice("Command syntax: /FrameSendRate <number>".L10N("Client:Main:ChatboxCommandFrameSendRateSyntax"));
                return;
            }

            FrameSendRate = intValue;
            AddNotice(string.Format(CultureInfo.CurrentCulture, "FrameSendRate has been changed to {0}".L10N("Client:Main:FrameSendRateChanged"), intValue));

            await OnGameOptionChangedAsync().ConfigureAwait(false);
            ClearReadyStatuses();
            ClearReadyStatuses();
        }

        private async ValueTask SetMaxAheadAsync(string value)
        {
            bool success = int.TryParse(value, out int intValue);

            if (!success)
            {
                AddNotice("Command syntax: /MaxAhead <number>".L10N("Client:Main:ChatboxCommandMaxAheadSyntax"));
                return;
            }

            MaxAhead = intValue;
            AddNotice(string.Format(CultureInfo.CurrentCulture, "MaxAhead has been changed to {0}".L10N("Client:Main:MaxAheadChanged"), intValue));

            await OnGameOptionChangedAsync().ConfigureAwait(false);
            ClearReadyStatuses();
        }

        private async ValueTask SetProtocolVersionAsync(string value)
        {
            bool success = int.TryParse(value, out int intValue);

            if (!success)
            {
                AddNotice("Command syntax: /ProtocolVersion <number>.".L10N("Client:Main:ChatboxCommandProtocolVersionSyntax"));
                return;
            }

            if (!(intValue == 0 || intValue == 2))
            {
                AddNotice("ProtocolVersion only allows values 0 and 2.".L10N("Client:Main:ChatboxCommandProtocolVersionInvalid"));
                return;
            }

            ProtocolVersion = intValue;
            AddNotice(string.Format(CultureInfo.CurrentCulture, "ProtocolVersion has been changed to {0}".L10N("Client:Main:ProtocolVersionChanged"), intValue));

            await OnGameOptionChangedAsync().ConfigureAwait(false);
            ClearReadyStatuses();
        }

        private async ValueTask SetStartingLocationClearanceAsync(string value)
        {
            bool removeStartingLocations = Conversions.BooleanFromString(value, RemoveStartingLocations);

            SetRandomStartingLocations(removeStartingLocations);

            await OnGameOptionChangedAsync().ConfigureAwait(false);
            ClearReadyStatuses();
        }

        /// <summary>
        /// Enables or disables completely random starting locations and informs
        /// the user accordingly.
        /// </summary>
        /// <param name="newValue">The new value of completely random starting locations.</param>
        protected void SetRandomStartingLocations(bool newValue)
        {
            if (newValue != RemoveStartingLocations)
            {
                RemoveStartingLocations = newValue;
                if (RemoveStartingLocations)
                    AddNotice("The game host has enabled completely random starting locations (only works for regular maps).".L10N("Client:Main:HostEnabledRandomStartLocation"));
                else
                    AddNotice("The game host has disabled completely random starting locations.".L10N("Client:Main:HostDisabledRandomStartLocation"));
            }
        }

        /// <summary>
        /// Handles the dice rolling command.
        /// </summary>
        /// <param name="dieType">The parameters given for the command by the user.</param>
        private ValueTask RollDiceCommandAsync(string dieType)
        {
            int dieSides = 6;
            int dieCount = 1;

            if (!string.IsNullOrEmpty(dieType))
            {
                string[] parts = dieType.Split('d');
                if (parts.Length == 2)
                {
                    if (!int.TryParse(parts[0], out dieCount) || !int.TryParse(parts[1], out dieSides))
                    {
                        AddNotice("Invalid dice specified. Expected format: /roll <die count>d<die sides>".L10N("Client:Main:ChatboxCommandRollInvalidAndSyntax"));
                        return default;
                    }
                }
            }

            if (dieCount > MAX_DICE || dieCount < 1)
            {
                AddNotice("You can only between 1 to 10 dies at once.".L10N("Client:Main:ChatboxCommandRollInvalid2"));
                return default;
            }

            if (dieSides > MAX_DIE_SIDES || dieSides < 2)
            {
                AddNotice("You can only have between 2 and 100 sides in a die.".L10N("Client:Main:ChatboxCommandRollInvalid3"));
                return default;
            }

            int[] results = new int[dieCount];
            Random random = new Random();
            for (int i = 0; i < dieCount; i++)
            {
                results[i] = random.Next(1, dieSides + 1);
            }

            return BroadcastDiceRollAsync(dieSides, results);
        }

        /// <summary>
        /// Handles custom map load command.
        /// </summary>
        /// <param name="mapName">Name of the map given as a parameter, without file extension.</param>
        private void LoadCustomMap(string mapName)
        {
            Map map = MapLoader.LoadCustomMap($"Maps/Custom/{mapName}", out string resultMessage);
            if (map != null)
            {
                AddNotice(resultMessage);
                ListMaps();
            }
            else
            {
                AddNotice(resultMessage, Color.Red);
            }
        }

        /// <summary>
        /// Override in derived classes to broadcast the results of rolling dice to other players.
        /// </summary>
        /// <param name="dieSides">The number of sides in the dice.</param>
        /// <param name="results">The results of the dice roll.</param>
        protected abstract ValueTask BroadcastDiceRollAsync(int dieSides, int[] results);

        /// <summary>
        /// Parses and lists the results of rolling dice.
        /// </summary>
        /// <param name="senderName">The player that rolled the dice.</param>
        /// <param name="result">The results of rolling dice, with each die separated by a comma
        /// and the number of sides in the die included as the first number.</param>
        /// <example>
        /// HandleDiceRollResult("Rampastring", "6,3,5,1") would mean that
        /// Rampastring rolled three six-sided dice and got 3, 5 and 1.
        /// </example>
        protected void HandleDiceRollResult(string senderName, string result)
        {
            if (string.IsNullOrEmpty(result))
                return;

            string[] parts = result.Split(',');
            if (parts.Length < 2 || parts.Length > MAX_DICE + 1)
                return;

            int[] intArray = Array.ConvertAll(parts, (s) => { return Conversions.IntFromString(s, -1); });
            int dieSides = intArray[0];
            if (dieSides < 1 || dieSides > MAX_DIE_SIDES)
                return;
            int[] results = new int[intArray.Length - 1];
            Array.ConstrainedCopy(intArray, 1, results, 0, results.Length);

            for (int i = 1; i < intArray.Length; i++)
            {
                if (intArray[i] < 1 || intArray[i] > dieSides)
                    return;
            }

            PrintDiceRollResult(senderName, dieSides, results);
        }

        /// <summary>
        /// Prints the result of rolling dice.
        /// </summary>
        /// <param name="senderName">The player who rolled dice.</param>
        /// <param name="dieSides">The number of sides in the die.</param>
        /// <param name="results">The results of the roll.</param>
        protected void PrintDiceRollResult(string senderName, int dieSides, int[] results)
        {
            AddNotice(String.Format(CultureInfo.CurrentCulture, "{0} rolled {1}d{2} and got {3}".L10N("Client:Main:PrintDiceRollResult"),
                senderName, results.Length, dieSides, string.Join(", ", results)
            ));
        }

        protected abstract ValueTask SendChatMessageAsync(string message);

        /// <summary>
        /// Changes the game lobby's UI depending on whether the local player is the host.
        /// </summary>
        /// <param name="isHost">Determines whether the local player is the host of the game.</param>
        protected void Refresh(bool isHost)
        {
            IsHost = isHost;
            Locked = false;
            CopyPlayerDataToUI();

            UpdateMapPreviewBoxEnabledStatus();
            PlayerExtraOptionsPanel?.SetIsHost(isHost);

            btnLaunchGame.Text = IsHost ? BTN_LAUNCH_GAME : BTN_LAUNCH_READY;

            if (IsHost)
            {
                ShowMapList();
                BtnSaveLoadGameOptions?.Enable();

                btnLockGame.Text = "Lock Game".L10N("Client:Main:ButtonLockGame");
                btnLockGame.Enabled = true;
                btnLockGame.Visible = true;
                chkAutoReady.Disable();

                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    dd.InputEnabled = true;
                    dd.SelectedIndex = dd.UserSelectedIndex;
                }

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                {
                    checkBox.AllowChanges = true;
                    checkBox.Checked = checkBox.UserChecked;
                }

                GenerateGameID();
            }
            else
            {
                HideMapList();
                BtnSaveLoadGameOptions?.Disable();

                btnLockGame.Enabled = false;
                btnLockGame.Visible = false;
                ReadINIForControl(chkAutoReady);

                foreach (GameLobbyDropDown dd in DropDowns)
                    dd.InputEnabled = false;

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                    checkBox.AllowChanges = false;
            }

            LoadDefaultGameModeMap();

            lbChatMessages.Clear();
            lbChatMessages.TopIndex = 0;

            lbChatMessages.AddItem("Type / to view a list of available chat commands.".L10N("Client:Main:ChatCommandTip"), Color.Silver, true);

            if (SavedGameManager.GetSaveGameCount() > 0)
            {
                lbChatMessages.AddItem(("Multiplayer saved games from a previous match have been detected. " +
                    "The saved games of the previous match will be deleted if you create new saves during this match.").L10N("Client:Main:SavedGameDetected"),
                    Color.Yellow, true);
            }
        }

        private void HideMapList()
        {
            lbChatMessages.Name = "lbChatMessages_Player";
            tbChatInput.Name = "tbChatInput_Player";
            MapPreviewBox.Name = "MapPreviewBox";
            lblMapName.Name = "lblMapName";
            lblMapAuthor.Name = "lblMapAuthor";
            lblGameMode.Name = "lblGameMode";
            lblMapSize.Name = "lblMapSize";

            ReadINIForControl(btnPickRandomMap);
            ReadINIForControl(lbChatMessages);
            ReadINIForControl(tbChatInput);
            ReadINIForControl(lbGameModeMapList);
            ReadINIForControl(lblMapName);
            ReadINIForControl(lblMapAuthor);
            ReadINIForControl(lblGameMode);
            ReadINIForControl(lblMapSize);
            ReadINIForControl(btnMapSortAlphabetically);

            ddGameModeMapFilter.Disable();
            lblGameModeSelect.Disable();
            lbGameModeMapList.Disable();
            tbMapSearch.Disable();
            btnPickRandomMap.Disable();
            btnMapSortAlphabetically.Disable();
        }

        private void ShowMapList()
        {
            lbChatMessages.Name = "lbChatMessages_Host";
            tbChatInput.Name = "tbChatInput_Host";
            MapPreviewBox.Name = "MapPreviewBox";
            lblMapName.Name = "lblMapName";
            lblMapAuthor.Name = "lblMapAuthor";
            lblGameMode.Name = "lblGameMode";
            lblMapSize.Name = "lblMapSize";

            ddGameModeMapFilter.Enable();
            lblGameModeSelect.Enable();
            lbGameModeMapList.Enable();
            tbMapSearch.Enable();
            btnPickRandomMap.Enable();
            btnMapSortAlphabetically.Enable();

            ReadINIForControl(btnPickRandomMap);
            ReadINIForControl(lbChatMessages);
            ReadINIForControl(tbChatInput);
            ReadINIForControl(lbGameModeMapList);
            ReadINIForControl(lblMapName);
            ReadINIForControl(lblMapAuthor);
            ReadINIForControl(lblGameMode);
            ReadINIForControl(lblMapSize);
            ReadINIForControl(btnMapSortAlphabetically);
        }

        private void MapPreviewBox_LocalStartingLocationSelected(object sender, LocalStartingLocationEventArgs e)
        {
            int mTopIndex = Players.FindIndex(p => string.Equals(p.Name, ProgramConstants.PLAYERNAME, StringComparison.OrdinalIgnoreCase));

            if (mTopIndex == -1 || Players[mTopIndex].SideId == ddPlayerSides[0].Items.Count - 1)
                return;

            ddPlayerStarts[mTopIndex].SelectedIndex = e.StartingLocationIndex;
        }

        private ValueTask MapPreviewBox_StartingLocationAppliedAsync()
        {
            ClearReadyStatuses();
            CopyPlayerDataToUI();
            return BroadcastPlayerOptionsAsync();
        }

        /// <summary>
        /// Handles the user's click on the "Launch Game" / "I'm Ready" button.
        /// If the local player is the game host, checks if the game can be launched and then
        /// launches the game if it's allowed. If the local player isn't the game host,
        /// sends a ready request.
        /// </summary>
        protected override async ValueTask BtnLaunchGame_LeftClickAsync()
        {
            if (!IsHost)
            {
                await RequestReadyStatusAsync().ConfigureAwait(false);
                return;
            }

            if (!Locked)
            {
                await LockGameNotificationAsync().ConfigureAwait(false);
                return;
            }

            var teamMappingsError = GetTeamMappingsError();
            if (!string.IsNullOrEmpty(teamMappingsError))
            {
                AddNotice(teamMappingsError);
                return;
            }

            List<int> occupiedColorIds = [];
            foreach (PlayerInfo player in Players)
            {
                if (occupiedColorIds.Contains(player.ColorId) && player.ColorId > 0)
                {
                    await SharedColorsNotificationAsync().ConfigureAwait(false);
                    return;
                }

                occupiedColorIds.Add(player.ColorId);
            }

            if (AIPlayers.Any(pInfo => pInfo.SideId == ddPlayerSides[0].Items.Count - 1))
            {
                await AISpectatorsNotificationAsync().ConfigureAwait(false);
                return;
            }

            if (Map.EnforceMaxPlayers)
            {
                foreach (PlayerInfo pInfo in Players)
                {
                    if (pInfo.StartingLocation == 0)
                        continue;

                    if (Players.Concat(AIPlayers).ToList().Find(
                        p => p.StartingLocation == pInfo.StartingLocation &&
                        !string.Equals(p.Name, pInfo.Name, StringComparison.OrdinalIgnoreCase)) != null)
                    {
                        await SharedStartingLocationNotificationAsync().ConfigureAwait(false);
                        return;
                    }
                }

                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
                    int startingLocation = AIPlayers[aiId].StartingLocation;

                    if (startingLocation == 0)
                        continue;

                    int index = AIPlayers.FindIndex(aip => aip.StartingLocation == startingLocation);

                    if (index > -1 && index != aiId)
                    {
                        await SharedStartingLocationNotificationAsync().ConfigureAwait(false);
                        return;
                    }
                }

                int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
                    + AIPlayers.Count;

                int minPlayers = GameMode.MinPlayersOverride > -1 ? GameMode.MinPlayersOverride : Map.MinPlayers;
                if (totalPlayerCount < minPlayers)
                {
                    await InsufficientPlayersNotificationAsync().ConfigureAwait(false);
                    return;
                }

                if (Map.EnforceMaxPlayers && totalPlayerCount > Map.MaxPlayers)
                {
                    await TooManyPlayersNotificationAsync().ConfigureAwait(false);
                    return;
                }
            }

            int iId = 0;
            foreach (PlayerInfo player in Players)
            {
                iId++;

                if (string.Equals(player.Name, ProgramConstants.PLAYERNAME, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!player.Verified)
                {
                    await NotVerifiedNotificationAsync(iId - 1).ConfigureAwait(false);
                    return;
                }

                if (player.IsInGame)
                {
                    await StillInGameNotificationAsync(iId - 1).ConfigureAwait(false);
                    return;
                }

                if (!player.Ready)
                {
                    await GetReadyNotificationAsync().ConfigureAwait(false);
                    return;
                }
            }

            await HostLaunchGameAsync().ConfigureAwait(false);
        }

        protected virtual ValueTask LockGameNotificationAsync()
        {
            AddNotice("You need to lock the game room before launching the game.".L10N("Client:Main:LockGameNotification"));

            return default;
        }

        protected virtual ValueTask SharedColorsNotificationAsync()
        {
            AddNotice("Multiple human players cannot share the same color.".L10N("Client:Main:SharedColorsNotification"));

            return default;
        }

        protected virtual ValueTask AISpectatorsNotificationAsync()
        {
            AddNotice("AI players don't enjoy spectating matches. They want some action!".L10N("Client:Main:AISpectatorsNotification"));

            return default;
        }

        protected virtual ValueTask SharedStartingLocationNotificationAsync()
        {
            AddNotice("Multiple players cannot share the same starting location on this map.".L10N("Client:Main:SharedStartingLocationNotification"));

            return default;
        }

        protected virtual ValueTask NotVerifiedNotificationAsync(int playerIndex)
        {
            if (playerIndex > -1 && playerIndex < Players.Count)
                AddNotice(string.Format(CultureInfo.CurrentCulture, "Unable to launch game. Player {0} hasn't been verified.".L10N("Client:Main:NotVerifiedNotification"), Players[playerIndex].Name));

            return default;
        }

        protected virtual ValueTask StillInGameNotificationAsync(int playerIndex)
        {
            if (playerIndex > -1 && playerIndex < Players.Count)
            {
                AddNotice(string.Format(CultureInfo.CurrentCulture, "Unable to launch game. Player {0} is still playing the game you started previously.".L10N("Client:Main:StillInGameNotification"),
                    Players[playerIndex].Name));
            }

            return default;
        }

        protected virtual ValueTask GetReadyNotificationAsync()
        {
            AddNotice("The host wants to start the game but cannot because not all players are ready!".L10N("Client:Main:GetReadyNotification"));
            if (!IsHost && !Players.Find(p => string.Equals(p.Name, ProgramConstants.PLAYERNAME, StringComparison.OrdinalIgnoreCase)).Ready)
                sndGetReadySound.Play();

            return default;
        }

        protected virtual ValueTask InsufficientPlayersNotificationAsync()
        {
            if (GameMode != null && GameMode.MinPlayersOverride > -1)
                AddNotice(String.Format(CultureInfo.CurrentCulture, "Unable to launch game: {0} cannot be played with fewer than {1} players".L10N("Client:Main:InsufficientPlayersNotification1"),
                    GameMode.UIName, GameMode.MinPlayersOverride));
            else if (Map != null)
                AddNotice(String.Format(CultureInfo.CurrentCulture, "Unable to launch game: this map cannot be played with fewer than {0} players.".L10N("Client:Main:InsufficientPlayersNotification2"),
                    Map.MinPlayers));

            return default;
        }

        protected virtual ValueTask TooManyPlayersNotificationAsync()
        {
            if (Map != null)
                AddNotice(String.Format(CultureInfo.CurrentCulture, "Unable to launch game: this map cannot be played with more than {0} players.".L10N("Client:Main:TooManyPlayersNotification"),
                    Map.MaxPlayers));

            return default;
        }

        public virtual ValueTask ClearAsync(bool exiting)
        {
            if (!IsHost)
                AIPlayers.Clear();

            Players.Clear();

            return default;
        }

        protected override async ValueTask OnGameOptionChangedAsync()
        {
            await base.OnGameOptionChangedAsync().ConfigureAwait(false);

            ClearReadyStatuses();
            CopyPlayerDataToUI();
        }

        protected abstract ValueTask HostLaunchGameAsync();

        protected override async ValueTask CopyPlayerDataFromUIAsync(object sender)
        {
            if (PlayerUpdatingInProgress)
                return;

            if (IsHost)
            {
                await base.CopyPlayerDataFromUIAsync(sender).ConfigureAwait(false);
                await BroadcastPlayerOptionsAsync().ConfigureAwait(false);
                return;
            }

            int mTopIndex = Players.FindIndex(p => string.Equals(p.Name, ProgramConstants.PLAYERNAME, StringComparison.OrdinalIgnoreCase));

            if (mTopIndex == -1)
                return;

            int requestedSide = ddPlayerSides[mTopIndex].SelectedIndex;
            int requestedColor = ddPlayerColors[mTopIndex].SelectedIndex;
            int requestedStart = ddPlayerStarts[mTopIndex].SelectedIndex;
            int requestedTeam = ddPlayerTeams[mTopIndex].SelectedIndex;

            await RequestPlayerOptionsAsync(requestedSide, requestedColor, requestedStart, requestedTeam).ConfigureAwait(false);
        }

        protected override void CopyPlayerDataToUI()
        {
            if (Players.Count + AIPlayers.Count > MAX_PLAYER_COUNT)
                return;

            base.CopyPlayerDataToUI();

            ClearPingIndicators();

            if (IsHost)
            {
                for (int pId = 1; pId < Players.Count; pId++)
                    ddPlayerNames[pId].AllowDropDown = true;
            }

            // Player statuses
            for (int pId = 0; pId < Players.Count; pId++)
            {
                if (Players[pId].IsInGame) // If player is ingame
                {
                    StatusIndicators[pId].SwitchTexture(PlayerSlotState.InGame);
                }
                else if (pId == 0) // If player is host
                {
                    StatusIndicators[pId].SwitchTexture(Locked ? PlayerSlotState.Ready : PlayerSlotState.NotReady); // Display room lock
                }
                else
                {
                    StatusIndicators[pId].SwitchTexture(Players[pId].Ready ? PlayerSlotState.Ready : PlayerSlotState.NotReady);
                }

                UpdatePlayerPingIndicator(Players[pId]);
            }

            // AI statuses
            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                StatusIndicators[aiId + Players.Count].SwitchTexture(
                    IsPlayerSpectator(AIPlayers[aiId]) ? PlayerSlotState.Error : PlayerSlotState.AI);

                if (IsPlayerSpectator(AIPlayers[aiId]))
                    StatusIndicators[aiId + Players.Count].ToolTip.Text += Environment.NewLine + "AI players can't be spectators.".L10N("Client:ClientGUI:AICantSpec");
            }

            // Empty slot statuses
            for (int i = AIPlayers.Count + Players.Count; i < MAX_PLAYER_COUNT; i++)
            {
                StatusIndicators[i].SwitchTexture(PlayerSlotState.Empty);
            }
        }

        protected virtual void ClearPingIndicators()
        {
            foreach (XNAClientDropDown dd in ddPlayerNames)
            {
                dd.Items[0].Texture = null;
                dd.ToolTip.Text = string.Empty;
            }
        }

        protected virtual void UpdatePlayerPingIndicator(PlayerInfo pInfo)
        {
            XNAClientDropDown ddPlayerName = ddPlayerNames[pInfo.Index];
            ddPlayerName.Items[0].Texture = GetTextureForPing(pInfo.Ping);
            if (pInfo.Ping < 0)
                ddPlayerName.ToolTip.Text = "Ping:".L10N("Client:Main:PlayerInfoPing") + " ? " + "ms".L10N("Client:Main:MillisecondsShort");
            else
                ddPlayerName.ToolTip.Text = "Ping:".L10N("Client:Main:PlayerInfoPing") + $" {pInfo.Ping} " + "ms".L10N("Client:Main:MillisecondsShort");
        }

        private Texture2D GetTextureForPing(int ping)
        {
            switch (ping)
            {
                case int p when (p > 350):
                    return PingTextures[4];
                case int p when (p > 250):
                    return PingTextures[3];
                case int p when (p > 100):
                    return PingTextures[2];
                case int p when (p >= 0):
                    return PingTextures[1];
                default:
                    return PingTextures[0];
            }
        }

        protected abstract ValueTask BroadcastPlayerOptionsAsync();

        protected abstract ValueTask BroadcastPlayerExtraOptionsAsync();

        protected abstract ValueTask RequestPlayerOptionsAsync(int side, int color, int start, int team);

        protected abstract ValueTask RequestReadyStatusAsync();

        // this public as it is used by the main lobby to notify the user of invitation failure
        public void AddWarning(string message)
        {
            AddNotice(message, Color.Yellow);
        }

        protected override bool AllowPlayerOptionsChange() => IsHost;

        protected override async ValueTask ChangeMapAsync(GameModeMap gameModeMap)
        {
            await base.ChangeMapAsync(gameModeMap).ConfigureAwait(false);

            bool resetAutoReady = gameModeMap?.GameMode == null || gameModeMap.Map == null;

            ClearReadyStatuses(resetAutoReady);

            if ((lastMapChangeWasInvalid || resetAutoReady) && chkAutoReady.Checked)
                await RequestReadyStatusAsync().ConfigureAwait(false);

            lastMapChangeWasInvalid = resetAutoReady;
        }

        protected override void ToggleFavoriteMap()
        {
            base.ToggleFavoriteMap();

            if (GameModeMap.IsFavorite || !IsHost)
                return;

            RefreshForFavoriteMapRemoved();
        }

        protected override void WriteSpawnIniAdditions(IniFile iniFile)
        {
            base.WriteSpawnIniAdditions(iniFile);
            iniFile.SetIntValue("Settings", "FrameSendRate", FrameSendRate);
            if (MaxAhead > 0)
                iniFile.SetIntValue("Settings", "MaxAhead", MaxAhead);
            iniFile.SetIntValue("Settings", "Protocol", ProtocolVersion);
        }

        protected override int GetDefaultMapRankIndex(GameModeMap gameModeMap)
        {
            if (gameModeMap.Map.MaxPlayers > 3)
                return StatisticsManager.Instance.GetCoopRankForDefaultMap(gameModeMap.Map.UntranslatedName, gameModeMap.Map.MaxPlayers);

            if (StatisticsManager.Instance.HasWonMapInPvP(gameModeMap.Map.UntranslatedName, gameModeMap.GameMode.UntranslatedUIName, gameModeMap.Map.MaxPlayers))
                return 2;

            return -1;
        }

        public void SwitchOn() => Enable();

        public void SwitchOff() => Disable();

        public abstract string GetSwitchName();

        protected override void UpdateMapPreviewBoxEnabledStatus()
        {
            if (Map != null && GameMode != null)
            {
                bool disablestartlocs = (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations || GetPlayerExtraOptions().IsForceRandomStarts);
                MapPreviewBox.EnableContextMenu = disablestartlocs ? false : IsHost;
                MapPreviewBox.EnableStartLocationSelection = !disablestartlocs;
            }
            else
            {
                MapPreviewBox.EnableContextMenu = IsHost;
                MapPreviewBox.EnableStartLocationSelection = true;
            }
        }

        protected override bool UpdateLaunchGameButtonStatus()
        {
            if (IsHost)
                btnLaunchGame.Enabled = base.UpdateLaunchGameButtonStatus() && GameMode != null && Map != null;
            else
                btnLaunchGame.Enabled = base.UpdateLaunchGameButtonStatus() && !chkAutoReady.Checked;

            return btnLaunchGame.Enabled;
        }
    }
}