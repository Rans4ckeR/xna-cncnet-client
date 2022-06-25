﻿using System.Collections.Generic;
using ClientCore;
using ClientGUI;
using DTAConfig.Settings;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels;

/// <summary>
/// A base class for all option panels. Handles custom game-specific panel options defined in INI files.
/// </summary>
internal abstract class XNAOptionsPanel : XNAWindowBase
{
    private static readonly OptionsGUICreator OptionsGUICreator = new();

    private readonly List<IUserSetting> userSettings = new();

    public XNAOptionsPanel(
        WindowManager windowManager,
        UserINISettings iniSettings)
        : base(windowManager)
    {
        IniSettings = iniSettings;
        CustomGUICreator = OptionsGUICreator;
    }

    protected UserINISettings IniSettings { get; private set; }

    public override void AddChild(XNAControl child)
    {
        base.AddChild(child);

        if (child is IUserSetting setting)
            userSettings.Add(setting);
    }

    public override void Initialize()
    {
        ClientRectangle = new Rectangle(
            12,
            47,
            Parent.Width - 24,
            Parent.Height - 94);
        BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
        PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

        base.Initialize();
    }

    /// <summary>
    /// Loads the options of this panel.
    /// </summary>
    public virtual void Load()
    {
        foreach (IUserSetting setting in userSettings)
            setting.Load();
    }

    /// <summary>
    /// Parses user-defined game options from an INI file.
    /// </summary>
    /// <param name="iniFile">The INI file.</param>
    public void ParseUserOptions(IniFile iniFile)
    {
        GetAttributes(iniFile);
        ParseExtraControls(iniFile, Name + "ExtraControls");
        ReadChildControlAttributes(iniFile);
    }

    /// <summary>
    /// Refreshes the panel's settings to account for possible changes that could affect the functionality.
    /// </summary>
    /// <returns>A bool that determines whether the setting's value was changed.</returns>
    public virtual bool RefreshPanel()
    {
        bool valuesChanged = false;
        foreach (IUserSetting setting in userSettings)
        {
            if (setting is IFileSetting fileSetting)
                valuesChanged = fileSetting.RefreshSetting() || valuesChanged;
        }

        return valuesChanged;
    }

    /// <summary>
    /// Saves the options of this panel. <returns>A bool that determines whether the client needs to
    /// restart for changes to apply.</returns>
    /// </summary>
    /// <returns>result.</returns>
    public virtual bool Save()
    {
        bool restartRequired = false;
        foreach (IUserSetting setting in userSettings)
            restartRequired = setting.Save() || restartRequired;

        return restartRequired;
    }

    /// <summary>
    /// Enables or disables any options that should only be available when options window was opened
    /// in main menu.
    /// </summary>
    /// <param name="enable">If true enables options, disables if false.</param>
    public virtual void ToggleMainMenuOnlyOptions(bool enable)
    {
    }
}