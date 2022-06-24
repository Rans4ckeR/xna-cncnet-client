﻿using System;
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

    public override void Write()
    {
        IniFile.SetStringValue(IniSection, IniKey, string.Join(",", Get()));
    }

    protected override List<string> Get()
    {
        string value = IniFile.GetStringValue(IniSection, IniKey, string.Empty);
        return string.IsNullOrWhiteSpace(value) ? DefaultValue : value.Split(',').ToList();
    }

    protected override void Set(List<string> value)
    {
        IniFile.SetStringValue(IniSection, IniKey, string.Join(",", value));
    }

    public void Add(string value)
    {
        List<string> values = Get().Concat(new[] { value }).ToList();
        Set(values);
    }

    public void Remove(string value)
    {
        List<string> values = Get().Where(v => !string.Equals(v, value, StringComparison.OrdinalIgnoreCase)).ToList();
        Set(values);
    }
}