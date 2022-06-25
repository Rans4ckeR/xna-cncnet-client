namespace DTAClient.Domain;

public partial class StatisticsSender
{
    private class GameURLInfo
    {
        public GameURLInfo(string gameId, string updateUrl, string cncnetUrl)
        {
            GameID = gameId;
            UpdateURL = updateUrl;
            CnCNetURL = cncnetUrl;
        }

        public string CnCNetURL { get; set; }

        public string GameID { get; set; }

        public string UpdateURL { get; set; }
    }
}