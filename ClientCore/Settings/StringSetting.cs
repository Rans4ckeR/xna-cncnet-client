using Rampastring.Tools;

namespace ClientCore.Settings;

public class StringSetting : INISetting<string>
{
    public StringSetting(IniFile iniFile, string iniSection, string iniKey, string defaultValue)
        : base(iniFile, iniSection, iniKey, defaultValue)
    {
    }

    public override string ToString()
    {
        return GetValue();
    }

    public override void Write()
    {
        IniFile.SetStringValue(IniSection, IniKey, GetValue());
    }

    protected override string GetValue()
    {
        return IniFile.GetStringValue(IniSection, IniKey, DefaultValue);
    }

    protected override void SetValue(string value)
    {
        IniFile.SetStringValue(IniSection, IniKey, value);
    }
}