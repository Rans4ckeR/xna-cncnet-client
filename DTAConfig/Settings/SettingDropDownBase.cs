﻿using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.Settings;

public abstract class SettingDropDownBase : XNAClientDropDown, IUserSetting
{
    private string _settingKey;

    private string _settingSection;

    public SettingDropDownBase(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public SettingDropDownBase(WindowManager windowManager, int defaultValue, string settingSection, string settingKey, bool restartRequired = false)
        : base(windowManager)
    {
        DefaultValue = defaultValue;
        SettingSection = settingSection;
        SettingKey = settingKey;
        RestartRequired = restartRequired;
    }

    public int DefaultValue { get; set; }

    public bool RestartRequired { get; set; }

    public string SettingKey
    {
        get => string.IsNullOrEmpty(_settingKey) ? $"{Name}{DefaultKeySuffix}" : _settingKey;
        set => _settingKey = value;
    }

    public string SettingSection
    {
        get => string.IsNullOrEmpty(_settingSection) ? DefaultSection : _settingSection;
        set => _settingSection = value;
    }

    protected string DefaultKeySuffix { get; set; } = "_SelectedIndex";

    protected string DefaultSection { get; set; } = "CustomSettings";

    protected int OriginalState { get; set; }

    public abstract void Load();

    public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "Items":
                string[] items = value.Split(',');
                for (int i = 0; i < items.Length; i++)
                {
                    XNADropDownItem item = new()
                    {
                        Text = items[i]
                    };
                    AddItem(item);
                }

                return;

            case "DefaultValue":
                DefaultValue = Conversions.IntFromString(value, 0);
                return;

            case "SettingSection":
                SettingSection = string.IsNullOrEmpty(value) ? SettingSection : value;
                return;

            case "SettingKey":
                SettingKey = string.IsNullOrEmpty(value) ? SettingKey : value;
                return;

            case "RestartRequired":
                RestartRequired = Conversions.BooleanFromString(value, false);
                return;
        }

        base.ParseAttributeFromINI(iniFile, key, value);
    }

    public abstract bool Save();
}