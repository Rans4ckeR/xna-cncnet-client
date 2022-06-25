using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Rampastring.Tools;

namespace Localization;

public class TranslationTable : ICloneable
{
    // As the ini value can not contains NewLine character '\n', it will be replaced with '@@' pattern.
    public static readonly string IniNewLinePattern = "@@";

    private readonly HashSet<string> notifiedMissingLabelsSet = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationTable"/> class.
    /// Create an empty translation table.
    /// </summary>
    public TranslationTable()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationTable"/> class.
    /// Load the translation table from an ini file.
    /// </summary>
    /// <param name="ini">An ini file to be read.</param>
    public TranslationTable(IniFile ini!!)
    {
        IniSection general = ini.GetSection("General");
        LanguageTag = general?.GetStringValue("LanguageTag", null);
        LanguageName = general?.GetStringValue("LanguageName", null);
        string cultureInfoName = general?.GetStringValue("CultureInfo", null);
        Author = general?.GetStringValue("Author", string.Empty);
        IniSection translation = ini.GetSection("Translation");

        if (general == null || translation == null || LanguageTag == null
            || LanguageName == null || cultureInfoName == null)
        {
            throw new InvalidDataException("Invalid translation table file.");
        }

        CultureInfo = new CultureInfo(cultureInfoName);

        foreach (KeyValuePair<string, string> kv in translation.Keys)
        {
            string label = kv.Key;
            string value = kv.Value;

            value = UnescapeIniValue(value);
            Table.Add(label, value);
        }
    }

    public TranslationTable(TranslationTable table)
    {
        LanguageTag = table.LanguageTag;
        LanguageName = table.LanguageName;
        Author = table.Author;
        CultureInfo = table.CultureInfo;
        foreach (KeyValuePair<string, string> kv in table.Table)
        {
            Table.Add(kv.Key, kv.Value);
        }
    }

    /// <summary>
    /// Get notified when a translation table does not contain a label that is needed.
    /// </summary>
    public event EventHandler<MissingTranslationEventArgs> MissingTranslationEvent;

    public static TranslationTable Instance { get; set; } = new TranslationTable();

    /// <summary>
    /// Gets or sets the internal ID for a language. Should be unique.
    /// It is recommended to use BCP-47 Language Tags.
    /// </summary>
    public string LanguageTag { get; set; } = "en-US";

    /// <summary>
    /// Gets or sets the user-friendly name for a language.
    /// </summary>
    public string LanguageName { get; set; } = "English (United States)";

    /// <summary>
    /// Gets the translation table. The key stands for a label name, and the value stands for a string that is used in System.string.Format().
    /// It is advised that the label name is started with "UI:" prefix.
    /// The value can not contains IniNewLinePattern when loading or saving via ini format.
    /// </summary>
    public Dictionary<string, string> Table { get; } = new Dictionary<string, string>();

    public CultureInfo CultureInfo { get; set; } = new CultureInfo("en-US");

    /// <summary>
    /// Gets or sets this a string showing the information about the authors. The program will not depend on this string.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    // public bool IsRightToLeft { get; set; } // TODO
    public static TranslationTable LoadFromIniFile(string iniPath)
    {
        using FileStream stream = File.Open(iniPath, FileMode.Open);
        IniFile iniFile = new(stream);
        return new TranslationTable(iniFile);
    }

    public static string EscapeIniValue(string raw)
    {
        if (raw.Contains(IniNewLinePattern))
            throw new InvalidDataException($"Pattern {IniNewLinePattern} is forbidden as this pattern is used to represent the new line.");

        if (raw.Contains(";"))
            throw new InvalidDataException("The semi-colon(;) is forbidden as this pattern is used to represent a comment line.");

        string value = raw.Replace(Environment.NewLine, "\n");
        value = value.Replace("\n", IniNewLinePattern);
        return value;
    }

    public static string UnescapeIniValue(string escaped)
        => escaped.Replace(IniNewLinePattern, "\n");

    public TranslationTable Clone() => new(this);

    object ICloneable.Clone() => Clone();

    /// <summary>
    /// Dump the translation table to an ini file.
    /// </summary>
    /// <returns>An ini file that contains the translation table.</returns>
    public IniFile SaveIni()
    {
        IniFile ini = new();

        ini.AddSection("General");
        IniSection general = ini.GetSection("General");
        general.AddKey("LanguageTag", LanguageTag);
        general.AddKey("LanguageName", LanguageName);
        general.AddKey("CultureInfo", CultureInfo.Name);
        general.AddKey("Author", Author);

        ini.AddSection("Translation");
        IniSection translation = ini.GetSection("Translation");

        foreach (KeyValuePair<string, string> kv in Table)
        {
            string label = kv.Key;
            string value = kv.Value;

            value = EscapeIniValue(value);

            translation.AddKey(label, value);
        }

        return ini;
    }

    public string GetTableValue(string label, string defaultValue)
    {
        if (Table.ContainsKey(label))
        {
            return Table[label];
        }
        else
        {
            OnMissingTranslationEvent(this, new MissingTranslationEventArgs(LanguageTag, label, defaultValue));
            return defaultValue;
        }
    }

    private void OnMissingTranslationEvent(object sender, MissingTranslationEventArgs e!!)
    {
        if (notifiedMissingLabelsSet.Contains(e.Label))
            return;
        MissingTranslationEvent?.Invoke(this, e);
        _ = notifiedMissingLabelsSet.Add(e.Label);
    }
}