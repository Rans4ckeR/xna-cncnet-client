using System.Collections.Generic;
using System.Linq;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// Handles game option presets.
/// </summary>
public class GameOptionPresets
{
    private const string IniFileName = "GameOptionsPresets.ini";
    private const string PresetDefinitionsSectionName = "Presets";

    private static GameOptionPresets _instance;

    private IniFile gameOptionPresetsIni;

    private Dictionary<string, GameOptionPreset> presets;

    private GameOptionPresets()
    {
    }

    public static GameOptionPresets Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GameOptionPresets();

            return _instance;
        }
    }

    public void AddPreset(GameOptionPreset preset)
    {
        LoadIniIfNotInitialized();

        presets[preset.ProfileName] = preset;
        WriteIni();
    }

    public void DeletePreset(string name)
    {
        LoadIniIfNotInitialized();

        if (!presets.ContainsKey(name))
            return;

        _ = presets.Remove(name);
        WriteIni();
    }

    public GameOptionPreset GetPreset(string name)
    {
        LoadIniIfNotInitialized();

        if (presets.TryGetValue(name, out GameOptionPreset value))
        {
            return value;
        }

        return null;
    }

    public List<string> GetPresetNames()
    {
        LoadIniIfNotInitialized();

        return presets.Keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToList();
    }

    private void LoadIni()
    {
        gameOptionPresetsIni = new IniFile(ProgramConstants.ClientUserFilesPath + IniFileName);
        presets = new Dictionary<string, GameOptionPreset>();

        IniSection presetsDefinitions = gameOptionPresetsIni.GetSection(PresetDefinitionsSectionName);
        if (presetsDefinitions == null)
            return;

        foreach (KeyValuePair<string, string> kvp in presetsDefinitions.Keys)
        {
            if (!presets.ContainsKey(kvp.Value))
            {
                IniSection presetSection = gameOptionPresetsIni.GetSection(kvp.Value);
                if (presetSection == null)
                    continue;

                GameOptionPreset preset = new(kvp.Value);
                preset.Read(presetSection);
                presets[kvp.Value] = preset;
            }
        }
    }

    private void LoadIniIfNotInitialized()
    {
        if (gameOptionPresetsIni == null)
            LoadIni();
    }

    private void WriteIni()
    {
        gameOptionPresetsIni = new IniFile();
        int i = 0;
        IniSection definitionsSection = new(PresetDefinitionsSectionName);
        gameOptionPresetsIni.AddSection(definitionsSection);
        foreach (KeyValuePair<string, GameOptionPreset> kvp in presets)
        {
            definitionsSection.SetStringValue(i.ToString(), kvp.Value.ProfileName);
            IniSection presetSection = new(kvp.Value.ProfileName);
            kvp.Value.Write(presetSection);
            gameOptionPresetsIni.AddSection(presetSection);
            i++;
        }

        gameOptionPresetsIni.WriteIniFile(ProgramConstants.ClientUserFilesPath + IniFileName);
    }
}