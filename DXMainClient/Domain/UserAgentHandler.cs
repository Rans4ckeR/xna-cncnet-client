using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ClientCore;

namespace DTAClient.Domain;

internal class UserAgentHandler
{
    private const int URLMON_OPTION_USERAGENT = 0x10000001;

    public static void ChangeUserAgent()
    {
        string ua = "DTA Client/" + Application.ProductVersion + "/Game " + ClientConfiguration.Instance.LocalGame + Environment.OSVersion.VersionString;

        _ = UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, ua, ua.Length, 0);
    }

    [DllImport("urlmon.dll", CharSet = CharSet.Unicode)]
    private static extern int UrlMkSetSessionOption(
        int dwOption, string pBuffer, int dwBufferLength, int dwReserved);
}