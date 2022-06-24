namespace DTAConfig.Settings;

internal interface IFileSetting : IUserSetting
{
    /// <summary>
    /// Gets a value indicating whether determines if the setting availability is checked on runtime.
    /// </summary>
    bool CheckAvailability { get; }

    /// <summary>
    /// Gets a value indicating whether determines if the client would adjust the setting value automatically
    /// if the current value becomes unavailable.
    /// </summary>
    bool ResetUnavailableValue { get; }

    /// <summary>
    /// Refreshes the setting to account for possible
    /// changes that could affect it's functionality.
    /// </summary>
    /// <returns>A bool that determines whether the
    /// setting's value was changed.</returns>
    bool RefreshSetting();
}