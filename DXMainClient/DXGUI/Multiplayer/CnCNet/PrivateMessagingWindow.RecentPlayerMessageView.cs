using ClientGUI;
using DTAClient.Online;
using Localization;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

public partial class PrivateMessagingWindow
{
    private class RecentPlayerMessageView : IMessageView
    {
        private readonly WindowManager windowManager;

        public RecentPlayerMessageView(WindowManager windowManager)
        {
            this.windowManager = windowManager;
        }

        public void AddMessage(ChatMessage message)
            => XNAMessageBox.Show(windowManager, "Message".L10N("UI:Main:MessageTitle"), message.Message);
    }
}