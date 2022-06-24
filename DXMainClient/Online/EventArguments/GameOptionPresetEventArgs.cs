using System;

namespace DTAClient.Online.EventArguments;

public class GameOptionPresetEventArgs : EventArgs
{
    public GameOptionPresetEventArgs(string presetName)
    {
        PresetName = presetName;
    }

    public string PresetName { get; }
}