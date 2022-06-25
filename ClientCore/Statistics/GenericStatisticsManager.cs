using System.Collections.Generic;
using System.IO;

namespace ClientCore.Statistics;

public abstract class GenericStatisticsManager
{
    protected List<MatchStatistics> Statistics { get; set; } = new();

    public MatchStatistics GetMatchByIndex(int index)
    {
        return Statistics[index];
    }

    public int GetMatchCount()
    {
        return Statistics.Count;
    }

    public abstract void ReadStatistics(string gamePath);

    protected static string GetStatDatabaseVersion(string scorePath)
    {
        if (!File.Exists(scorePath))
        {
            return null;
        }

        using StreamReader reader = new(scorePath);
        char[] versionBuffer = new char[4];
        _ = reader.Read(versionBuffer, 0, versionBuffer.Length);

        string s = new(versionBuffer);
        return s;
    }
}