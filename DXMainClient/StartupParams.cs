using System.Collections.Generic;

namespace DTAClient;

/// <summary>
/// Contains client startup parameters.
/// </summary>
internal struct StartupParams
{
    public StartupParams(
        bool noAudio,
        bool multipleInstanceMode,
        List<string> unknownParams)
    {
        NoAudio = noAudio;
        MultipleInstanceMode = multipleInstanceMode;
        UnknownStartupParams = unknownParams;
    }

    public bool NoAudio { get; }

    public bool MultipleInstanceMode { get; }

    public List<string> UnknownStartupParams { get; }
}