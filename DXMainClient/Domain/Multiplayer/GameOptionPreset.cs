using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// A single game option preset.
/// </summary>
public class GameOptionPreset
{
    private readonly Dictionary<string, bool> checkBoxValues = new();

    private readonly Dictionary<string, int> dropDownValues = new();

    public GameOptionPreset(string profileName)
    {
        ProfileName = profileName;

        if (ProfileName.Contains('[') || ProfileName.Contains(']'))
            throw new ArgumentException("Game option preset name cannot contain the [] characters.");
    }

    public string ProfileName { get; }

    /// <summary>
    /// Checks if a specific name is valid for the name of a game option preset. Returns null if the
    /// name is valid, an error message otherwise.
    /// </summary>
    /// <param name="name">name.</param>
    /// <returns>result.</returns>
    public static string IsNameValid(string name)
    {
        if (name.Contains('[') || name.Contains(']'))
            return "Game option preset name cannot contain the [] characters.";

        return null;
    }

    public void AddCheckBoxValue(string checkBoxName, bool value)
    {
        checkBoxValues.Add(checkBoxName, value);
    }

    public void AddDropDownValue(string dropDownValue, int value)
    {
        dropDownValues.Add(dropDownValue, value);
    }

    public Dictionary<string, bool> GetCheckBoxValues() => new(checkBoxValues);

    public Dictionary<string, int> GetDropDownValues() => new(dropDownValues);

    public void Read(IniSection section)
    {
        // Syntax example: CheckBoxValues=chkCrates:1,chkShortGame:1,chkFastResourceGrowth:0,.... (0
        // = unchecked, 1 = checked) DropDownValues=ddTechLevel:7,ddStartingCredits:5,... (the
        // number is the selected option index)
        AddValues(section, "CheckBoxValues", checkBoxValues, s => s == "1");
        AddValues(section, "DropDownValues", dropDownValues, s => Conversions.IntFromString(s, 0));
    }

    public void Write(IniSection section)
    {
        section.SetStringValue("CheckBoxValues", string.Join(
            ",",
            checkBoxValues.Select(s => $"{s.Key}:{(s.Value ? "1" : "0")}")));
        section.SetStringValue("DropDownValues", string.Join(
            ",",
            dropDownValues.Select(s => $"{s.Key}:{s.Value}")));
    }

    private void AddValues<T>(IniSection section, string keyName, Dictionary<string, T> dictionary, Converter<string, T> converter)
    {
        string[] valueStrings = section.GetStringValue(
            keyName,
            string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string value in valueStrings)
        {
            string[] splitValue = value.Split(':');
            if (splitValue.Length != 2)
            {
                Logger.Log($"Failed to parse game option preset value ({ProfileName}, {keyName})");
                continue;
            }

            dictionary.Add(splitValue[0], converter(splitValue[1]));
        }
    }
}