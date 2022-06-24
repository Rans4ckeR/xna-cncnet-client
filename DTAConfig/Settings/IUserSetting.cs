namespace DTAConfig.Settings;

internal interface IUserSetting
{
    /// <summary>
    /// Gets iNI section name in user settings file this setting's value is stored in.
    /// </summary>
    string SettingSection { get; }

    /// <summary>
    /// Gets iNI key name in user settings file this setting's value is stored in.
    /// </summary>
    string SettingKey { get; }

    /// <summary>
    /// Gets a value indicating whether determines if this setting requires the client to be restarted
    /// in order to be correctly applied.
    /// </summary>
    bool RestartRequired { get; }

    /// <summary>
    /// Loads the current value for the user setting.
    /// </summary>
    void Load();

    /// <summary>
    /// Applies operations based on current setting state.
    /// </summary>
    /// <returns>A bool that determines whether the
    /// client needs to restart for changes to apply.</returns>
    bool Save();
}