using DTAClient.Online;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

public partial class PrivateMessagingWindow
{
    /// <summary>
    /// A class for storing a private message in memory.
    /// </summary>
    private class PrivateMessage
    {
        public PrivateMessage(IRCUser user, string message)
        {
            User = user;
            Message = message;
        }

        public string Message { get; set; }

        public IRCUser User { get; set; }
    }
}