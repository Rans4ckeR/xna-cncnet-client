using Rampastring.Tools;

namespace ClientCore.Settings;

public class BoolSetting : INISetting<bool>
{
    public BoolSetting(IniFile iniFile, string iniSection, string iniKey, bool defaultValue)
        : base(iniFile, iniSection, iniKey, defaultValue)
    {
    }

    public override void Write()
    {
        IniFile.SetBooleanValue(IniSection, IniKey, GetValue());
    }

    public override string ToString()
    {
        return GetValue().ToString();
    }

    protected override bool GetValue()
    {
        return IniFile.GetBooleanValue(IniSection, IniKey, DefaultValue);
    }

    protected override void SetValue(bool value)
    {
        IniFile.SetBooleanValue(IniSection, IniKey, value);
    }
}