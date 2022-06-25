using ClientCore;

namespace DTAClient.Domain;

public static class MainClientConstants
{
    public const string CNCNETTUNNELLISTURL = "http://cncnet.org/master-list";

    public static string CreditsUrl { get; set; } = "http://rampastring.cncnet.org/TS/Credits.txt";

    public static string GameNameLong { get; set; } = "CnCNet Client";

    public static string GameNameShort { get; set; } = "CnCNet";

    public static int MapCellSizeX { get; set; } = 48;

    public static int MapCellSizeY { get; set; } = 24;

    public static OSVersion OSId { get; set; } = OSVersion.UNKNOWN;

    public static string SupportUrlShort { get; set; } = "www.cncnet.org";

    public static void Initialize()
    {
        ClientConfiguration clientConfiguration = ClientConfiguration.Instance;

        OSId = ClientConfiguration.GetOperatingSystemVersion();

        GameNameShort = clientConfiguration.LocalGame;
        GameNameLong = clientConfiguration.LongGameName;

        SupportUrlShort = clientConfiguration.ShortSupportURL;

        CreditsUrl = clientConfiguration.CreditsURL;

        MapCellSizeX = clientConfiguration.MapCellSizeX;
        MapCellSizeY = clientConfiguration.MapCellSizeY;

        if (string.IsNullOrEmpty(GameNameShort))
            throw new ClientConfigurationException("LocalGame is set to an empty value.");

        if (GameNameShort.Length > ProgramConstants.GAMEIDMAXLENGTH)
        {
            throw new ClientConfigurationException("LocalGame is set to a value that exceeds length limit of " +
                ProgramConstants.GAMEIDMAXLENGTH + " characters.");
        }
    }
}