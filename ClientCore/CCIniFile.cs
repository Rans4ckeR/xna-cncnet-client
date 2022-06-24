using System.IO;
using Rampastring.Tools;

namespace ClientCore;

public class CCIniFile : IniFile
{
    public CCIniFile(string path)
        : base(path)
    {
        foreach (IniSection section in Sections)
        {
            string baseSectionName = section.GetStringValue("$BaseSection", null);

            if (string.IsNullOrWhiteSpace(baseSectionName))
                continue;

            IniSection baseSection = Sections.Find(s => s.SectionName == baseSectionName);
            if (baseSection == null)
            {
                Logger.Log($"Base section not found in INI file {path}, section {section.SectionName}, base section name: {baseSectionName}");
                continue;
            }

            int addedKeyCount = 0;

            foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in baseSection.Keys)
            {
                if (!section.KeyExists(kvp.Key))
                {
                    section.Keys.Insert(addedKeyCount, kvp);
                    addedKeyCount++;
                }
            }
        }
    }

    protected override void ApplyBaseIni()
    {
        string basedOnSetting = GetStringValue("INISystem", "BasedOn", string.Empty);
        if (string.IsNullOrEmpty(basedOnSetting))
            return;

        string[] basedOns = basedOnSetting.Split(',');
        foreach (string basedOn in basedOns)
            ApplyBasedOnIni(basedOn);
    }

    private void ApplyBasedOnIni(string basedOn)
    {
        if (string.IsNullOrEmpty(basedOn))
            return;

        string path = basedOn.Contains("$THEME_DIR$")
            ? basedOn.Replace("$THEME_DIR$", ProgramConstants.GetResourcePath().TrimEnd(new char[] { '/', '\\' }))
            : Path.GetDirectoryName(FileName) + "/" + basedOn;

        // Consolidate with the INI file that this INI file is based on
        if (!File.Exists(path))
            Logger.Log(FileName + ": Base INI file not found! " + path);

        CCIniFile baseIni = new(path);
        ConsolidateIniFiles(baseIni, this);
        Sections = baseIni.Sections;
    }
}