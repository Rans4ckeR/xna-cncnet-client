using System;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAConfig.Settings;

public abstract class SettingCheckBoxBase : XNAClientCheckBox, IUserSetting
{
    private XNAClientCheckBox _parentCheckBox;

    private string _parentCheckBoxName;

    private string _settingKey;

    private string _settingSection;

    public SettingCheckBoxBase(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public SettingCheckBoxBase(WindowManager windowManager, bool defaultValue, string settingSection, string settingKey, bool restartRequired = false)
        : base(windowManager)
    {
        DefaultValue = defaultValue;
        SettingSection = settingSection;
        SettingKey = settingKey;
        RestartRequired = restartRequired;
    }

    public bool DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets parent check-box control.
    /// </summary>
    public XNAClientCheckBox ParentCheckBox
    {
        get
        {
            return _parentCheckBox;
        }

        set
        {
            UpdateParentCheckBox(value);
            _parentCheckBoxName = _parentCheckBox?.Name;
        }
    }

    /// <summary>
    /// Gets or sets name of parent check-box control.
    /// </summary>
    public string ParentCheckBoxName
    {
        get
        {
            return _parentCheckBoxName;
        }

        set
        {
            _parentCheckBoxName = value;
            UpdateParentCheckBox(FindParentCheckBox());
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether value required from parent check-box control if set.
    /// </summary>
    public bool ParentCheckBoxRequiredValue { get; set; } = true;

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

    protected string DefaultKeySuffix { get; set; } = "_Checked";

    protected string DefaultSection { get; set; } = "CustomSettings";

    protected bool OriginalState { get; set; }

    public abstract void Load();

    public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "Checked":
            case "DefaultValue":
                DefaultValue = Conversions.BooleanFromString(value, false);
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

            case "ParentCheckBoxName":
                ParentCheckBoxName = value;
                return;

            case "ParentCheckBoxRequiredValue":
                ParentCheckBoxRequiredValue = Conversions.BooleanFromString(value, true);
                return;
        }

        base.ParseAttributeFromINI(iniFile, key, value);
    }

    public abstract bool Save();

    private XNAClientCheckBox FindParentCheckBox()
    {
        if (string.IsNullOrEmpty(ParentCheckBoxName))
            return null;

        foreach (Rampastring.XNAUI.XNAControls.XNAControl control in Parent.Children)
        {
            if (control is XNAClientCheckBox && control.Name == ParentCheckBoxName)
                return control as XNAClientCheckBox;
        }

        return null;
    }

    private void ParentCheckBox_CheckedChanged(object sender, EventArgs e) => UpdateAllowChecking();

    private void UpdateAllowChecking()
    {
        if (ParentCheckBox != null)
        {
            if (ParentCheckBox.Checked == ParentCheckBoxRequiredValue)
            {
                AllowChecking = true;
            }
            else
            {
                AllowChecking = false;
                Checked = false;
            }
        }
    }

    private void UpdateParentCheckBox(XNAClientCheckBox parentCheckBox)
    {
        if (ParentCheckBox != null)
            ParentCheckBox.CheckedChanged -= ParentCheckBox_CheckedChanged;

        _parentCheckBox = parentCheckBox;
        UpdateAllowChecking();

        if (ParentCheckBox != null)
            ParentCheckBox.CheckedChanged += ParentCheckBox_CheckedChanged;
    }
}