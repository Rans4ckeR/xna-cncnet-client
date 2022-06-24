using Rampastring.Tools;

namespace ClientCore.Settings;

/// <summary>
/// A base class for an INI setting.
/// </summary>
public abstract class INISetting<T> : IIniSetting
{
    public INISetting(IniFile iniFile, string iniSection, string iniKey,
        T defaultValue)
    {
        IniFile = iniFile;
        IniSection = iniSection;
        IniKey = iniKey;
        DefaultValue = defaultValue;
    }

    public T Value
    {
        get { return Get(); }
        set { Set(value); }
    }

    protected IniFile IniFile { get; private set; }

    public static implicit operator T(INISetting<T> iniSetting)
    {
        return iniSetting.Get();
    }

    public void SetIniFile(IniFile iniFile)
    {
        IniFile = iniFile;
    }

    protected string IniSection { get; private set; }

    protected string IniKey { get; private set; }

    protected T DefaultValue { get; private set; }

    /// <summary>
    /// Writes the default value of this setting to the INI file if no value
    /// for the setting is currently specified in the INI file.
    /// </summary>
    public void SetDefaultIfNonexistent()
    {
        if (!IniFile.KeyExists(IniSection, IniKey))
            Set(DefaultValue);
    }

    public abstract void Write();

    protected abstract T Get();

    protected abstract void Set(T value);
}