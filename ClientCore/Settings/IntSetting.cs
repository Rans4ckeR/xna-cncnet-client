using Rampastring.Tools;

namespace ClientCore.Settings;

public class IntSetting : INISetting<int>
{
    public IntSetting(IniFile iniFile, string iniSection, string iniKey, int defaultValue)
        : base(iniFile, iniSection, iniKey, defaultValue)
    {
    }

    public override string ToString()
    {
        return GetValue().ToString();
    }

    public override void Write()
    {
        IniFile.SetIntValue(IniSection, IniKey, GetValue());
    }

    protected override int GetValue()
    {
        return IniFile.GetIntValue(IniSection, IniKey, DefaultValue);
    }

    protected override void SetValue(int value)
    {
        IniFile.SetIntValue(IniSection, IniKey, value);
    }
}