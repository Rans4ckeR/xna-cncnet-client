using System;
using System.Collections.Generic;
using System.IO;
using ClientCore;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig;

/// <summary>
/// A window for configuring in-game hotkeys.
/// </summary>
public partial class HotkeyConfigurationWindow : XNAWindow
{
    private const string KEYBOARD_COMMANDS_INI = "KeyboardCommands.ini";
    private readonly List<GameCommand> gameCommands = new();
    private readonly string hOTKEY_TIP_TEXT = "Press a key...".L10N("UI:DTAConfig:PressAKey");

    /// <summary>
    /// Keys that the client doesn't allow to be used regular hotkeys.
    /// </summary>
    private readonly Keys[] keyBlacklist = new Keys[]
    {
        Keys.LeftAlt,
        Keys.RightAlt,
        Keys.LeftControl,
        Keys.RightControl,
        Keys.LeftShift,
        Keys.RightShift
    };

    private XNAClientButton btnResetKey;

    private XNAClientDropDown ddCategory;

    private XNAPanel hotkeyInfoPanel;

    private IniFile keyboardINI;

    private KeyModifiers lastFrameModifiers;

    private XNAMultiColumnListBox lbHotkeys;

    private XNALabel lblCommandCaption;

    private XNALabel lblCurrentHotkeyValue;

    private XNALabel lblCurrentlyAssignedTo;

    private XNALabel lblDefaultHotkeyValue;

    private XNALabel lblDescription;

    private XNALabel lblNewHotkeyValue;

    private Hotkey pendingHotkey;

    public HotkeyConfigurationWindow(WindowManager windowManager)
                                                            : base(windowManager)
    {
    }

    public override void Initialize()
    {
        ReadGameCommands();

        Name = "HotkeyConfigurationWindow";
        ClientRectangle = new Rectangle(0, 0, 600, 450);
        BackgroundTexture = AssetLoader.LoadTextureUncached("hotkeyconfigbg.png");

        XNALabel lblCategory = new(WindowManager)
        {
            Name = "lblCategory",
            ClientRectangle = new Rectangle(12, 12, 0, 0),
            Text = "Category:".L10N("UI:DTAConfig:Category")
        };

        ddCategory = new XNAClientDropDown(WindowManager)
        {
            Name = "ddCategory"
        };
        ddCategory.ClientRectangle = new Rectangle(
            lblCategory.Right + 12,
            lblCategory.Y - 1,
            250,
            ddCategory.Height);

        HashSet<string> categories = new();

        foreach (GameCommand command in gameCommands)
        {
            if (!categories.Contains(command.Category))
                _ = categories.Add(command.Category);
        }

        foreach (string category in categories)
            ddCategory.AddItem(category);

        lbHotkeys = new XNAMultiColumnListBox(WindowManager)
        {
            Name = "lbHotkeys",
            ClientRectangle = new Rectangle(
                12,
                ddCategory.Bottom + 12,
                ddCategory.Right - 12,
                Height - ddCategory.Bottom - 59),
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED,
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1)
        };
        lbHotkeys.AddColumn("Command".L10N("UI:DTAConfig:Command"), 150);
        lbHotkeys.AddColumn("Shortcut".L10N("UI:DTAConfig:Shortcut"), lbHotkeys.Width - 150);

        hotkeyInfoPanel = new XNAPanel(WindowManager)
        {
            Name = "HotkeyInfoPanel",
            ClientRectangle = new Rectangle(
                lbHotkeys.Right + 12,
                ddCategory.Y,
                Width - lbHotkeys.Right - 24,
                lbHotkeys.Height + ddCategory.Height + 12)
        };

        lblCommandCaption = new XNALabel(WindowManager)
        {
            Name = "lblCommandCaption",
            FontIndex = 1,
            ClientRectangle = new Rectangle(12, 12, 0, 0),
            Text = "Command name".L10N("UI:DTAConfig:CommandName")
        };

        lblDescription = new XNALabel(WindowManager)
        {
            Name = "lblDescription",
            ClientRectangle = new Rectangle(12, lblCommandCaption.Bottom + 12, 0, 0),
            Text = "Command description".L10N("UI:DTAConfig:CommandDescription")
        };

        XNALabel lblCurrentHotkey = new(WindowManager)
        {
            Name = "lblCurrentHotkey",
            ClientRectangle = new Rectangle(
                lblDescription.X,
                lblDescription.Bottom + 48,
                0,
                0),
            FontIndex = 1,
            Text = "Currently assigned hotkey:".L10N("UI:DTAConfig:CurrentHotKey")
        };

        lblCurrentHotkeyValue = new XNALabel(WindowManager)
        {
            Name = "lblCurrentHotkeyValue",
            ClientRectangle = new Rectangle(
                lblDescription.X,
                lblCurrentHotkey.Bottom + 6,
                0,
                0),
            Text = "Current hotkey value".L10N("UI:DTAConfig:CurrentHotKeyValue")
        };

        XNALabel lblNewHotkey = new(WindowManager)
        {
            Name = "lblNewHotkey",
            ClientRectangle = new Rectangle(
                lblDescription.X,
                lblCurrentHotkeyValue.Bottom + 48,
                0,
                0),
            FontIndex = 1,
            Text = "New hotkey:".L10N("UI:DTAConfig:NewHotKey")
        };

        lblNewHotkeyValue = new XNALabel(WindowManager)
        {
            Name = "lblNewHotkeyValue",
            ClientRectangle = new Rectangle(
                lblDescription.X,
                lblNewHotkey.Bottom + 6,
                0,
                0),
            Text = hOTKEY_TIP_TEXT
        };

        lblCurrentlyAssignedTo = new XNALabel(WindowManager)
        {
            Name = "lblCurrentlyAssignedTo",
            ClientRectangle = new Rectangle(
                lblDescription.X,
                lblNewHotkeyValue.Bottom + 12,
                0,
                0),
            Text = "Currently assigned to:".L10N("UI:DTAConfig:CurrentHotKeyAssign") + "\nKey"
        };

        XNAClientButton btnAssign = new(WindowManager)
        {
            Name = "btnAssign",
            ClientRectangle = new Rectangle(
                lblDescription.X,
                lblCurrentlyAssignedTo.Bottom + 24,
                UIDesignConstants.ButtonWidth121,
                UIDesignConstants.ButtonHeight),
            Text = "Assign Hotkey".L10N("UI:DTAConfig:AssignHotkey")
        };
        btnAssign.LeftClick += BtnAssign_LeftClick;

        btnResetKey = new XNAClientButton(WindowManager)
        {
            Name = "btnResetKey",
            ClientRectangle = new Rectangle(btnAssign.X, btnAssign.Bottom + 12, btnAssign.Width, 23),
            Text = "Reset to Default".L10N("UI:DTAConfig:ResetToDefault")
        };
        btnResetKey.LeftClick += BtnReset_LeftClick;

        XNALabel lblDefaultHotkey = new(WindowManager)
        {
            Name = "lblOriginalHotkey",
            ClientRectangle = new Rectangle(lblCurrentHotkey.X, btnResetKey.Bottom + 12, 0, 0),
            Text = "Default hotkey:".L10N("UI:DTAConfig:DefaultHotKey")
        };

        lblDefaultHotkeyValue = new XNALabel(WindowManager)
        {
            Name = "lblDefaultHotkeyValue",
            ClientRectangle = new Rectangle(lblDefaultHotkey.Right + 12, lblDefaultHotkey.Y, 0, 0)
        };

        XNAClientButton btnSave = new(WindowManager)
        {
            Name = "btnSave",
            ClientRectangle = new Rectangle(12, lbHotkeys.Bottom + 12, UIDesignConstants.ButtonWidth92, UIDesignConstants.ButtonHeight),
            Text = "Save".L10N("UI:DTAConfig:ButtonSave")
        };
        btnSave.LeftClick += BtnSave_LeftClick;

        XNAClientButton btnResetAllKeys = new(WindowManager)
        {
            Name = "btnResetAllToDefaults",
            ClientRectangle = new Rectangle(0, btnSave.Y, UIDesignConstants.ButtonWidth121, UIDesignConstants.ButtonHeight),
            Text = "Reset All Keys".L10N("UI:DTAConfig:ResetAllHotkey")
        };
        btnResetAllKeys.LeftClick += BtnResetToDefaults_LeftClick;
        AddChild(btnResetAllKeys);
        btnResetAllKeys.CenterOnParentHorizontally();

        XNAClientButton btnCancel = new(WindowManager)
        {
            Name = "btnExit",
            ClientRectangle = new Rectangle(Width - 104, btnSave.Y, UIDesignConstants.ButtonWidth92, UIDesignConstants.ButtonHeight),
            Text = "Cancel".L10N("UI:DTAConfig:ButtonCancel")
        };
        btnCancel.LeftClick += BtnCancel_LeftClick;

        AddChild(lbHotkeys);
        AddChild(lblCategory);
        AddChild(ddCategory);
        AddChild(hotkeyInfoPanel);
        AddChild(btnSave);
        AddChild(btnCancel);
        hotkeyInfoPanel.AddChild(lblCommandCaption);
        hotkeyInfoPanel.AddChild(lblDescription);
        hotkeyInfoPanel.AddChild(lblCurrentHotkey);
        hotkeyInfoPanel.AddChild(lblCurrentHotkeyValue);
        hotkeyInfoPanel.AddChild(lblNewHotkey);
        hotkeyInfoPanel.AddChild(lblNewHotkeyValue);
        hotkeyInfoPanel.AddChild(lblCurrentlyAssignedTo);
        hotkeyInfoPanel.AddChild(lblDefaultHotkey);
        hotkeyInfoPanel.AddChild(lblDefaultHotkeyValue);
        hotkeyInfoPanel.AddChild(btnAssign);
        hotkeyInfoPanel.AddChild(btnResetKey);

        if (categories.Count > 0)
        {
            hotkeyInfoPanel.Disable();
            lbHotkeys.SelectedIndexChanged += LbHotkeys_SelectedIndexChanged;

            ddCategory.SelectedIndexChanged += DdCategory_SelectedIndexChanged;
            ddCategory.SelectedIndex = 0;
        }
        else
        {
            Logger.Log("No keyboard game commands exist!");
        }

        GameProcessLogic.GameProcessExited += GameProcessLogic_GameProcessExited;

        base.Initialize();

        CenterOnParent();

        Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
        EnabledChanged += HotkeyConfigurationWindow_EnabledChanged;
    }

    /// <summary>
    /// Updates the logic of the window. Used for keeping the "new hotkey" display in sync with the
    /// keyboard's modifier keys.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        KeyModifiers oldModifiers = pendingHotkey.Modifier;
        KeyModifiers currentModifiers = GetCurrentModifiers();

        if ((pendingHotkey.Key == Keys.None && currentModifiers != oldModifiers)
            ||
            (pendingHotkey.Key != Keys.None &&
            lastFrameModifiers == KeyModifiers.None &&
            currentModifiers != lastFrameModifiers))
        {
            pendingHotkey = new Hotkey(Keys.None, currentModifiers);
            lblCurrentlyAssignedTo.Text = string.Empty;
        }

        string displayString = pendingHotkey.ToString();
        lblNewHotkeyValue.Text = displayString != string.Empty ? pendingHotkey.ToString() : hOTKEY_TIP_TEXT;

        lastFrameModifiers = currentModifiers;
    }

    /// <summary>
    /// Allows defining keys that match other keys for in-game purposes and should be displayed as
    /// those keys instead.
    /// </summary>
    /// <param name="key">The key.</param>
    private static Keys GetKeyOverride(Keys key)
    {
        // 12 is actually NumPad5 for the game
        if (key == (Keys)12)
            return Keys.NumPad5;

        return key;
    }

    private void BtnAssign_LeftClick(object sender, EventArgs e)
    {
        if (lbHotkeys.SelectedIndex < 0 || lbHotkeys.SelectedIndex >= lbHotkeys.ItemCount)
        {
            return;
        }

        // If the hotkey is already assigned to other command, unbind it
        foreach (GameCommand gameCommand in gameCommands)
        {
            if (pendingHotkey.Equals(gameCommand.Hotkey))
                gameCommand.Hotkey = new Hotkey(Keys.None, KeyModifiers.None);
        }

        GameCommand command = (GameCommand)lbHotkeys.GetItem(0, lbHotkeys.SelectedIndex).Tag;
        command.Hotkey = pendingHotkey;
        RefreshHotkeyList();
        pendingHotkey = new Hotkey(Keys.None, KeyModifiers.None);
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Disable();
    }

    /// <summary>
    /// Resets the hotkey for the currently selected game command to its default value.
    /// </summary>
    private void BtnReset_LeftClick(object sender, EventArgs e)
    {
        if (lbHotkeys.SelectedIndex < 0 || lbHotkeys.SelectedIndex >= lbHotkeys.ItemCount)
        {
            return;
        }

        GameCommand command = (GameCommand)lbHotkeys.GetItem(0, lbHotkeys.SelectedIndex).Tag;
        command.Hotkey = command.DefaultHotkey;

        // If the hotkey is already assigned to some other command, unbind it
        foreach (GameCommand gameCommand in gameCommands)
        {
            if (pendingHotkey.Equals(gameCommand.Hotkey))
                gameCommand.Hotkey = new Hotkey(Keys.None, KeyModifiers.None);
        }

        pendingHotkey = new Hotkey(Keys.None, KeyModifiers.None);
        RefreshHotkeyList();
    }

    private void BtnResetToDefaults_LeftClick(object sender, EventArgs e)
    {
        foreach (GameCommand command in gameCommands)
        {
            command.Hotkey = command.DefaultHotkey;
        }

        RefreshHotkeyList();
    }

    private void BtnSave_LeftClick(object sender, EventArgs e)
    {
        WriteKeyboardINI();
        Disable();
    }

    private void DdCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        lbHotkeys.ClearItems();
        lbHotkeys.TopIndex = 0;
        string category = ddCategory.SelectedItem.Text;
        foreach (GameCommand command in gameCommands)
        {
            if (command.Category == category)
            {
                lbHotkeys.AddItem(new XNAListBoxItem[]
                {
                    new XNAListBoxItem() { Text = command.UIName, Tag = command },
                    new XNAListBoxItem() { Text = command.Hotkey.ToString() }
                });
            }
        }

        lbHotkeys.SelectedIndex = -1;
    }

    /// <summary>
    /// Reloads Keyboard.ini when the game process has exited.
    /// </summary>
    private void GameProcessLogic_GameProcessExited()
    {
        WindowManager.AddCallback(new Action(LoadKeyboardINI), null);
    }

    /// <summary>
    /// Detects which key modifiers (Ctrl, Shift, Alt) the user is currently pressing.
    /// </summary>
    private KeyModifiers GetCurrentModifiers()
    {
        KeyModifiers currentModifiers = KeyModifiers.None;

        if (Keyboard.IsKeyHeldDown(Keys.RightControl) ||
            Keyboard.IsKeyHeldDown(Keys.LeftControl))
        {
            currentModifiers |= KeyModifiers.Ctrl;
        }

        if (Keyboard.IsKeyHeldDown(Keys.RightShift) ||
            Keyboard.IsKeyHeldDown(Keys.LeftShift))
        {
            currentModifiers |= KeyModifiers.Shift;
        }

        if (Keyboard.IsKeyHeldDown(Keys.LeftAlt) ||
            Keyboard.IsKeyHeldDown(Keys.RightAlt))
        {
            currentModifiers |= KeyModifiers.Alt;
        }

        return currentModifiers;
    }

    private void HotkeyConfigurationWindow_EnabledChanged(object sender, EventArgs e)
    {
        if (Enabled)
        {
            LoadKeyboardINI();
            RefreshHotkeyList();
        }
    }

    /// <summary>
    /// Detects when the user has pressed a key to generate a new hotkey.
    /// </summary>
    private void Keyboard_OnKeyPressed(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
    {
        foreach (Keys blacklistedKey in keyBlacklist)
        {
            if (e.PressedKey == blacklistedKey)
                return;
        }

        KeyModifiers currentModifiers = GetCurrentModifiers();

        // The XNA keys seem to match the Windows virtual keycodes! This saves us some work
        pendingHotkey = new Hotkey(HotkeyConfigurationWindow.GetKeyOverride(e.PressedKey), currentModifiers);

        lblCurrentlyAssignedTo.Text = string.Empty;

        foreach (GameCommand command in gameCommands)
        {
            if (pendingHotkey.Equals(command.Hotkey))
                lblCurrentlyAssignedTo.Text = "Currently assigned to:".L10N("UI:DTAConfig:CurrentAssignTo") + Environment.NewLine + command.UIName;
        }
    }

    private void LbHotkeys_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbHotkeys.SelectedIndex < 0 || lbHotkeys.SelectedIndex >= lbHotkeys.ItemCount)
        {
            hotkeyInfoPanel.Disable();
            return;
        }

        hotkeyInfoPanel.Enable();
        GameCommand command = (GameCommand)lbHotkeys.GetItem(0, lbHotkeys.SelectedIndex).Tag;
        lblCommandCaption.Text = command.UIName;
        lblDescription.Text = Renderer.FixText(
            command.Description,
            lblDescription.FontIndex,
            hotkeyInfoPanel.Width - lblDescription.X).Text;
        lblCurrentHotkeyValue.Text = command.Hotkey.ToStringWithNone();

        lblDefaultHotkeyValue.Text = command.DefaultHotkey.ToStringWithNone();
        btnResetKey.Enabled = !command.Hotkey.Equals(command.DefaultHotkey);

        lblNewHotkeyValue.Text = hOTKEY_TIP_TEXT;
        pendingHotkey = new Hotkey(Keys.None, KeyModifiers.None);
        lblCurrentlyAssignedTo.Text = string.Empty;
    }

    private void LoadKeyboardINI()
    {
        keyboardINI = new IniFile(ProgramConstants.GamePath + ClientConfiguration.Instance.KeyboardINI);

        if (File.Exists(ProgramConstants.GamePath + ClientConfiguration.Instance.KeyboardINI))
        {
            foreach (GameCommand command in gameCommands)
            {
                int hotkey = keyboardINI.GetIntValue("Hotkey", command.ININame, 0);

                Hotkey hotkeyStruct = new(hotkey);
                command.Hotkey = new Hotkey(HotkeyConfigurationWindow.GetKeyOverride(hotkeyStruct.Key), hotkeyStruct.Modifier);
            }
        }
        else
        {
            foreach (GameCommand command in gameCommands)
            {
                command.Hotkey = command.DefaultHotkey;
            }
        }
    }

    /// <summary>
    /// Reads game commands from an INI file.
    /// </summary>
    private void ReadGameCommands()
    {
        IniFile gameCommandsIni = new(ProgramConstants.GetBaseResourcePath() + KEYBOARD_COMMANDS_INI);

        List<string> sections = gameCommandsIni.GetSections();

        foreach (string sectionName in sections)
        {
            gameCommands.Add(new GameCommand(gameCommandsIni.GetSection(sectionName)));
        }
    }

    private void RefreshHotkeyList()
    {
        int selectedIndex = lbHotkeys.SelectedIndex;
        int topIndex = lbHotkeys.TopIndex;
        DdCategory_SelectedIndexChanged(null, EventArgs.Empty);
        lbHotkeys.TopIndex = topIndex;
        lbHotkeys.SelectedIndex = selectedIndex;
    }

    private void WriteKeyboardINI()
    {
        IniFile keyboardIni = new();
        foreach (GameCommand command in gameCommands)
        {
            keyboardIni.SetStringValue("Hotkey", command.ININame, command.Hotkey.GetTSEncoded().ToString());
        }

        keyboardIni.WriteIniFile(ProgramConstants.GamePath + ClientConfiguration.Instance.KeyboardINI);
    }
}