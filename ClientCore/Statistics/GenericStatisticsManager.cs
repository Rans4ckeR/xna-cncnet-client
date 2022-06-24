using System.Collections.Generic;
using System.IO;

namespace ClientCore.Statistics;

public abstract class GenericStatisticsManager
{
    protected List<MatchStatistics> statistics = new();

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

    public int GetMatchCount()
    {
        return statistics.Count;
    }

    public MatchStatistics GetMatchByIndex(int index)
    {
        return statistics[index];
    }
}