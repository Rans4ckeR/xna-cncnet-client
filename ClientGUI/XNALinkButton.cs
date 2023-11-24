using System;
using Rampastring.XNAUI;
using Rampastring.Tools;
using ClientCore;

namespace ClientGUI
{
    public class XNALinkButton : XNAClientButton
    {
        public XNALinkButton(WindowManager windowManager) : base(windowManager) { }

        public string URL { get; set; }
        public string UnixURL { get; set; }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (string.Equals(key, "URL", StringComparison.OrdinalIgnoreCase))
            {
                URL = value;
                return;
            }

            if (string.Equals(key, "UnixURL", StringComparison.OrdinalIgnoreCase))
            {
                UnixURL = value;
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnLeftClick()
        {
            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            if (osVersion == OSVersion.UNIX && !string.IsNullOrEmpty(UnixURL))
                ProcessLauncher.StartShellProcess(UnixURL);
            else if (!string.IsNullOrEmpty(URL))
                ProcessLauncher.StartShellProcess(URL);

            base.OnLeftClick();
        }
    }
}