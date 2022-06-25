using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.Tools;

namespace ClientCore.Settings;

/// <summary>
/// This is a setting that can be stored as a comma separated list of strings.
/// </summary>
public class StringListSetting : INISetting<List<string>>
{
    public StringListSetting(IniFile iniFile, string iniSection, string iniKey, List<string> defaultValue)
        : base(iniFile, iniSection, iniKey, defaultValue)
    {
    }

    public void Add(string value)
    {
        List<string> values = GetValue().Concat(new[] { value }).ToList();
        SetValue(values);
    }

    public void Remove(string value)
    {
        List<string> values = GetValue().Where(v => !string.Equals(v, value, StringComparison.OrdinalIgnoreCase)).ToList();
        SetValue(values);
    }

    public override void Write()
    {
        IniFile.SetStringValue(IniSection, IniKey, string.Join(",", GetValue()));
    }

    protected override List<string> GetValue()
    {
        string value = IniFile.GetStringValue(IniSection, IniKey, string.Empty);
        return string.IsNullOrWhiteSpace(value) ? DefaultValue : value.Split(',').ToList();
    }

    protected override void SetValue(List<string> value)
    {
        IniFile.SetStringValue(IniSection, IniKey, string.Join(",", value));
    }
}