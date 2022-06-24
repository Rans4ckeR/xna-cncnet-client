namespace ClientCore.Statistics;

public abstract class GenericMatchParser
{
    public GenericMatchParser(MatchStatistics ms)
    {
        Statistics = ms;
    }


    public MatchStatistics Statistics { get; set; }

    protected abstract void ParseStatistics(string gamepath);
}