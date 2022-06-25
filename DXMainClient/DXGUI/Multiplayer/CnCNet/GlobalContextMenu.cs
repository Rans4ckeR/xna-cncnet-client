using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using ClientCore;
using ClientCore.Extensions;
using ClientGUI;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

public class GlobalContextMenu : XNAContextMenu
{
    private readonly string aDD_FRIEND = "Add Friend".L10N("UI:Main:AddFriend");

    private readonly string bLOCK = "Block".L10N("UI:Main:Block");

    private readonly CnCNetUserData cncnetUserData;

    private readonly string cOPY_LINK = "Copy Link".L10N("UI:Main:CopyLink");

    private readonly string iNVITE = "Invite".L10N("UI:Main:Invite");

    private readonly string jOIN = "Join".L10N("UI:Main:Join");

    private readonly string oPEN_LINK = "Open Link".L10N("UI:Main:OpenLink");

    private readonly PrivateMessagingWindow pmWindow;

    private readonly string pRIVATE_MESSAGE = "Private Message".L10N("UI:Main:PrivateMessage");

    private readonly string rEMOVE_FRIEND = "Remove Friend".L10N("UI:Main:RemoveFriend");

    private readonly string uNBLOCK = "Unblock".L10N("UI:Main:Unblock");

    private XNAContextMenuItem copyLinkItem;

    private XNAContextMenuItem invitePlayerItem;

    private XNAContextMenuItem joinPlayerItem;

    private XNAContextMenuItem openLinkItem;

    private XNAContextMenuItem privateMessageItem;

    private XNAContextMenuItem toggleFriendItem;

    private XNAContextMenuItem toggleIgnoreItem;

    public GlobalContextMenu(
        WindowManager windowManager,
        CnCNetManager connectionManager,
        CnCNetUserData cncnetUserData,
        PrivateMessagingWindow pmWindow)
        : base(windowManager)
    {
        ConnectionManager = connectionManager;
        this.cncnetUserData = cncnetUserData;
        this.pmWindow = pmWindow;

        Name = nameof(GlobalContextMenu);
        ClientRectangle = new Rectangle(0, 0, 150, 2);
        Enabled = false;
        Visible = false;
    }

    public EventHandler<JoinUserEventArgs> JoinEvent { get; set; }

    protected CnCNetManager ConnectionManager { get; }

    protected GlobalContextMenuData ContextMenuData { get; set; }

    public override void Initialize()
    {
        privateMessageItem = new XNAContextMenuItem()
        {
            Text = pRIVATE_MESSAGE,
            SelectAction = () => pmWindow.InitPM(GetIrcUser().Name)
        };
        toggleFriendItem = new XNAContextMenuItem()
        {
            Text = aDD_FRIEND,
            SelectAction = () => cncnetUserData.ToggleFriend(GetIrcUser().Name)
        };
        toggleIgnoreItem = new XNAContextMenuItem()
        {
            Text = bLOCK,
            SelectAction = () => GetIrcUserIdent(cncnetUserData.ToggleIgnoreUser)
        };
        invitePlayerItem = new XNAContextMenuItem()
        {
            Text = iNVITE,
            SelectAction = Invite
        };
        joinPlayerItem = new XNAContextMenuItem()
        {
            Text = jOIN,
            SelectAction = () => JoinEvent?.Invoke(this, new JoinUserEventArgs(GetIrcUser()))
        };

        copyLinkItem = new XNAContextMenuItem()
        {
            Text = cOPY_LINK
        };

        openLinkItem = new XNAContextMenuItem()
        {
            Text = oPEN_LINK
        };

        AddItem(privateMessageItem);
        AddItem(toggleFriendItem);
        AddItem(toggleIgnoreItem);
        AddItem(invitePlayerItem);
        AddItem(joinPlayerItem);
        AddItem(copyLinkItem);
        AddItem(openLinkItem);
    }

    public void Show(string playerName, Point cursorPoint)
    {
        Show(
            new GlobalContextMenuData
            {
                PlayerName = playerName
            },
            cursorPoint);
    }

    public void Show(IRCUser ircUser, Point cursorPoint)
    {
        Show(
            new GlobalContextMenuData
            {
                IrcUser = ircUser
            },
            cursorPoint);
    }

    public void Show(ChannelUser channelUser, Point cursorPoint)
    {
        Show(
            new GlobalContextMenuData
            {
                ChannelUser = channelUser
            },
            cursorPoint);
    }

    public void Show(ChatMessage chatMessage, Point cursorPoint)
    {
        Show(
            new GlobalContextMenuData()
            {
                ChatMessage = chatMessage
            },
            cursorPoint);
    }

    public void Show(GlobalContextMenuData data, Point cursorPoint)
    {
        Disable();
        ContextMenuData = data;
        UpdateButtons();

        if (!Items.Any(i => i.Visible))
            return;

        Open(cursorPoint);
    }

    private void CopyLink(string link)
    {
        try
        {
            Clipboard.SetText(link);
        }
        catch (Exception)
        {
            XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Unable to copy link".L10N("UI:Main:ClipboardCopyLinkFailed"));
        }
    }

    private IRCUser GetIrcUser()
    {
        if (ContextMenuData.IrcUser != null)
            return ContextMenuData.IrcUser;

        if (ContextMenuData.ChannelUser?.IRCUser != null)
            return ContextMenuData.ChannelUser.IRCUser;

        if (!string.IsNullOrEmpty(ContextMenuData.PlayerName))
            return ConnectionManager.UserList.Find(u => u.Name == ContextMenuData.PlayerName);

        if (!string.IsNullOrEmpty(ContextMenuData.ChatMessage?.SenderName))
            return ConnectionManager.UserList.Find(u => u.Name == ContextMenuData.ChatMessage.SenderName);

        return null;
    }

    private void GetIrcUserIdent(Action<string> callback)
    {
        IRCUser ircUser = GetIrcUser();

        if (!string.IsNullOrEmpty(ircUser.Ident))
        {
            callback.Invoke(ircUser.Ident);
            return;
        }

        void WhoIsReply(object sender, WhoEventArgs whoEventargs)
        {
            ircUser.Ident = whoEventargs.Ident;
            callback.Invoke(whoEventargs.Ident);
            ConnectionManager.WhoReplyReceived -= WhoIsReply;
        }

        ConnectionManager.WhoReplyReceived += WhoIsReply;
        ConnectionManager.SendWhoIsMessage(ircUser.Name);
    }

    private void Invite()
    {
        // note it's assumed that if the channel name is specified, the game name must be also
        if (string.IsNullOrEmpty(ContextMenuData.InviteChannelName) || ProgramConstants.IsInGame)
        {
            return;
        }

        string messageBody = ProgramConstants.GAMEINVITECTCPCOMMAND + " " + ContextMenuData.InviteChannelName + ";" + ContextMenuData.InviteGameName;

        if (!string.IsNullOrEmpty(ContextMenuData.InviteChannelPassword))
        {
            messageBody += ";" + ContextMenuData.InviteChannelPassword;
        }

        ConnectionManager.SendCustomMessage(new QueuedMessage(
            "PRIVMSG " + GetIrcUser().Name + " :\u0001" + messageBody + "\u0001", QueuedMessageType.CHATMESSAGE, 0));
    }

    private void UpdateButtons()
    {
        UpdatePlayerBasedButtons();
        UpdateMessageBasedButtons();
    }

    private void UpdateMessageBasedButtons()
    {
        string link = ContextMenuData?.ChatMessage?.Message?.GetLink();

        copyLinkItem.Visible = link != null;
        openLinkItem.Visible = link != null;

        copyLinkItem.SelectAction = () =>
        {
            if (link == null)
                return;
            CopyLink(link);
        };
        openLinkItem.SelectAction = () =>
        {
            if (link == null)
                return;

            using Process proc = Process.Start(new ProcessStartInfo
            {
                FileName = link,
                UseShellExecute = true
            });
        };
    }

    private void UpdatePlayerBasedButtons()
    {
        IRCUser ircUser = GetIrcUser();
        bool isOnline = ircUser != null && ConnectionManager.UserList.Any(u => u.Name == ircUser.Name);
        bool isAdmin = ContextMenuData.ChannelUser?.IsAdmin ?? false;

        toggleFriendItem.Visible = ircUser != null;
        privateMessageItem.Visible = ircUser != null && isOnline;
        toggleIgnoreItem.Visible = ircUser != null;
        invitePlayerItem.Visible = ircUser != null && isOnline && !string.IsNullOrEmpty(ContextMenuData.InviteChannelName);
        joinPlayerItem.Visible = ircUser != null && !ContextMenuData.PreventJoinGame && isOnline;

        toggleIgnoreItem.Selectable = !isAdmin;

        if (ircUser == null)
            return;

        toggleFriendItem.Text = cncnetUserData.IsFriend(ircUser.Name) ? rEMOVE_FRIEND : aDD_FRIEND;
        toggleIgnoreItem.Text = cncnetUserData.IsIgnored(ircUser.Ident) ? uNBLOCK : bLOCK;
    }
}