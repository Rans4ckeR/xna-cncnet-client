using Rampastring.Tools;

namespace ClientCore.Settings;

public class DoubleSetting : INISetting<double>
{
    public DoubleSetting(IniFile iniFile, string iniSection, string iniKey, double defaultValue)
        : base(iniFile, iniSection, iniKey, defaultValue)
    {
    }

    public override void Write()
    {
        IniFile.SetDoubleValue(IniSection, IniKey, GetValue());
    }

    public override string ToString()
    {
        return GetValue().ToString();
    }

    protected override double GetValue()
    {
        return IniFile.GetDoubleValue(IniSection, IniKey, DefaultValue);
    }

    protected override void SetValue(double value)
    {
        IniFile.SetDoubleValue(IniSection, IniKey, value);
    }
}