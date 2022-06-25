using System;

namespace ClientCore;

public static class LoadingScreenController
{
    public static string GetLoadScreenName(string sideId)
    {
        int resHeight = UserINISettings.Instance.IngameScreenHeight;

        string loadingScreenName = ProgramConstants.BaseResourcePath + "l";

        if (resHeight < 480)
            loadingScreenName += "400";
        else if (resHeight < 600)
            loadingScreenName += "480";
        else
            loadingScreenName += "600";

        loadingScreenName = loadingScreenName + "s" + sideId;
        Random random = new();
        int randomInt = random.Next(1, 1 + ClientConfiguration.Instance.LoadingScreenCount);

        return loadingScreenName + Convert.ToString(randomInt) + ".pcx";
    }
}