using System.Collections.Generic;
using System.Linq;
using ClientCore;

namespace DTAClient.Domain;

/// <summary>
/// A class for sending statistics about updates and CnCNet to Google Analytics.
/// </summary>
public class StatisticsSender
{
    private static StatisticsSender _instance;

    private readonly List<GameURLInfo> urlInfos;

    private StatisticsSender()
    {
        //UserAgentHandler.ChangeUserAgent();
        //wb = new WebBrowser();
        //wb.ScriptErrorsSuppressed = true;
        GameURLInfo[] gameUrlInfos = new GameURLInfo[]
        {
            new GameURLInfo("DTA", "http://dta.ppmsite.com/ga-dta-update.htm", "http://dta.ppmsite.com/ga-dta-cncnet.htm"),
            new GameURLInfo("TI", "http://dta.ppmsite.com/ga-ti-update.htm", "http://dta.ppmsite.com/ga-ti-cncnet.htm"),
            new GameURLInfo("TS", "http://dta.ppmsite.com/ga-ts-update.htm", "http://dta.ppmsite.com/ga-ts-cncnet.htm"),
            new GameURLInfo("MO", "http://dta.ppmsite.com/ga-mo-update.htm", "http://dta.ppmsite.com/ga-mo-cncnet.htm"),
            new GameURLInfo("YR", "http://dta.ppmsite.com/ga-yr-update.htm", "http://dta.ppmsite.com/ga-yr-cncnet.htm"),
        };

        urlInfos = gameUrlInfos.ToList();

        myGameInfo = urlInfos.Find(g => g.GameID == ClientConfiguration.Instance.LocalGame);
    }

    public static StatisticsSender Instance
    {
        get
        {
            if (_instance == null)
                _instance = new StatisticsSender();

            return _instance;
        }
    }

    //private WebBrowser wb;
    private readonly GameURLInfo myGameInfo;

    public static void SendUpdate()
    {
        //if (myGameInfo == null)
        //    return;

        //try
        //{
        //    wb.Navigate(myGameInfo.UpdateURL);
        //}
        //catch (Exception ex)
        //{
        //    Logger.Log("Error sending statistics: " + ex.Message);
        //}
    }

    public static void SendCnCNet()
    {
        //if (myGameInfo == null)
        //    return;

        //try
        //{
        //    wb.Navigate(myGameInfo.CnCNetURL);
        //}
        //catch (Exception ex)
        //{
        //    Logger.Log("Error sending statistics: " + ex.Message);
        //}
    }

    private class GameURLInfo
    {
        public string GameID;

        public GameURLInfo(string gameId, string updateUrl, string cncnetUrl)
        {
            GameID = gameId;
            UpdateURL = updateUrl;
            CnCNetURL = cncnetUrl;
        }

        public string UpdateURL;
        public string CnCNetURL;
    }
}