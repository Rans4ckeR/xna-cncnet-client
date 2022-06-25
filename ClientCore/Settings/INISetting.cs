using Rampastring.Tools;

namespace ClientCore.Settings;

/// <summary>
/// A base class for an INI setting.
/// </summary>
/// <typeparam name="T">T.</typeparam>
public abstract class INISetting<T> : IIniSetting
{
    public INISetting(
        IniFile iniFile,
        string iniSection,
        string iniKey,
        T defaultValue)
    {
        IniFile = iniFile;
        IniSection = iniSection;
        IniKey = iniKey;
        DefaultValue = defaultValue;
    }

    public T Value
    {
        get => GetValue();
        set => SetValue(value);
    }

    protected T DefaultValue { get; private set; }

    protected IniFile IniFile { get; private set; }

    protected string IniKey { get; private set; }

    protected string IniSection { get; private set; }

    public static implicit operator T(INISetting<T> iniSetting)
    {
        return iniSetting.GetValue();
    }

    /// <summary>
    /// Writes the default value of this setting to the INI file if no value for the setting is
    /// currently specified in the INI file.
    /// </summary>
    public void SetDefaultIfNonexistent()
    {
        if (!IniFile.KeyExists(IniSection, IniKey))
            SetValue(DefaultValue);
    }

    public void SetIniFile(IniFile iniFile)
    {
        IniFile = iniFile;
    }

    public abstract void Write();

    protected abstract T GetValue();

    protected abstract void SetValue(T value);
}