using Rampastring.Tools;

namespace DTAConfig;

public partial class HotkeyConfigurationWindow
{
    /// <summary>
    /// A game command that can be assigned into a key on the keyboard.
    /// </summary>
    private class GameCommand
    {
        public GameCommand(string uiName, string category, string description, string iniName)
        {
            UIName = uiName;
            Category = category;
            Description = description;
            ININame = iniName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameCommand" /> class. Creates a game
        /// command and parses its information from an INI section.
        /// </summary>
        /// <param name="iniSection">The INI section.</param>
        public GameCommand(IniSection iniSection)
        {
            ININame = iniSection.SectionName;
            UIName = iniSection.GetStringValue("UIName", "Unnamed command");
            Category = iniSection.GetStringValue("Category", "Unknown category");
            Description = iniSection.GetStringValue("Description", "Unknown description");
            DefaultHotkey = new Hotkey(iniSection.GetIntValue("DefaultKey", 0));
        }

        public string Category { get; private set; }

        public Hotkey DefaultHotkey { get; private set; }

        public string Description { get; private set; }

        public Hotkey Hotkey { get; set; }

        public string ININame { get; private set; }

        public string UIName { get; private set; }

        /// <summary>
        /// Writes the game command's information to an INI file.
        /// </summary>
        /// <param name="iniFile">The INI file.</param>
        public void WriteToIni(IniFile iniFile)
        {
            IniSection section = new(ININame);
            section.SetStringValue("UIName", UIName);
            section.SetStringValue("Category", Category);
            section.SetStringValue("Description", Description);
            section.SetIntValue("DefaultKey", DefaultHotkey.GetTSEncoded());
            iniFile.AddSection(section);
        }
    }
}