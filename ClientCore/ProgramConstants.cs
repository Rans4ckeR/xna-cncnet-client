using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Localization;

#if !DEBUG
using System.IO;
#endif

namespace ClientCore;

/// <summary>
/// Contains various static variables and constants that the client uses for operation.
/// </summary>
public static class ProgramConstants
{
    public const string QRESEXECUTABLE = "qres.dat";

    public const string CNCNETPROTOCOLREVISION = "R9";

    /* For .NET 6 Release mode we split up the DXMainClient dll from the AppHost executable.
     * The AppHost is located in the root, as is the case for the .NET 4.8 executables.
     * The actual DXMainClient dll is 2 directories up in Application.StartupPath\Binaries\<WindowsGL,OpenGL,XNA> */
#if DEBUG
    public static readonly string GamePath = Application.StartupPath.Replace('\\', '/') + "/";
#elif NETFRAMEWORK
    public static readonly string GamePath = Directory.GetParent(Application.StartupPath.TrimEnd(new char[] { '\\' })).FullName.Replace('\\', '/') + "/";
#else
    public static readonly string GamePath = Directory.GetParent(Path.GetFullPath(Path.Combine(Application.StartupPath, "..\\..\\")).TrimEnd(new char[] { '\\' })).FullName.Replace('\\', '/') + "/";
#endif

    public static event EventHandler PlayerNameChanged;

    public static string ClientUserFilesPath => GamePath + "Client/";
    public const string LANPROTOCOLREVISION = "RL6";
    public const int LANPORT = 1234;
    public const int LANINGAMEPORT = 1234;
    public const int LANLOBBYPORT = 1232;
    public const int LANGAMELOBBYPORT = 1233;
    public const char LANDATASEPARATOR = (char)01;
    public const char LANMESSAGESEPARATOR = (char)02;

    public const string SPAWNMAPINI = "spawnmap.ini";
    public const string SPAWNERSETTINGS = "spawn.ini";
    public const string SAVEDGAMESPAWNINI = "Saved Games/spawnSG.ini";

    public const int GAMEIDMAXLENGTH = 4;

    public const string GAMEINVITECTCPCOMMAND = "INVITE";

    public const string GAMEINVITATIONFAILEDCTCPCOMMAND = "INVITATION_FAILED";

    public static readonly Encoding LANENCODING = Encoding.UTF8;

    public static readonly List<string> TEAMS = new() { "A", "B", "C", "D" };

    public static string GAME_VERSION = "Undefined";
    public static string BASE_RESOURCE_PATH = "Resources/";

    private static string playerName = "No name";

    public static string PLAYERNAME
    {
        get
        {
            return playerName;
        }

        set
        {
            string oldPlayerName = playerName;
            playerName = value;
            if (oldPlayerName != playerName)
                PlayerNameChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string RESOURCES_DIR = BASE_RESOURCE_PATH;

    public static int LOG_LEVEL = 1;

    public static bool IsInGame { get; set; }

    public static string GetResourcePath()
    {
        return GamePath + RESOURCES_DIR;
    }

    public static string GetBaseResourcePath()
    {
        return GamePath + BASE_RESOURCE_PATH;
    }

    public static string GetAILevelName(int aiLevel)
    {
        if (aiLevel > 0 && aiLevel < AIPLAYERNAMES.Count)
            return AIPLAYERNAMES[aiLevel];

        return string.Empty;
    }

    // Static fields might be initialized before the translation file is loaded. Change to readonly properties here.
    public static List<string> AIPLAYERNAMES => new() { "Easy AI".L10N("UI:Main:EasyAIName"), "Medium AI".L10N("UI:Main:MediumAIName"), "Hard AI".L10N("UI:Main:HardAIName") };
}