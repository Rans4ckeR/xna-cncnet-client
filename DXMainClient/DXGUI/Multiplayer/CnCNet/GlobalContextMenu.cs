﻿using System;
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
    public EventHandler<JoinUserEventArgs> JoinEvent;

    protected readonly CnCNetManager connectionManager;

    private readonly string pRIVATE_MESSAGE = "Private Message".L10N("UI:Main:PrivateMessage");
    private readonly string aDD_FRIEND = "Add Friend".L10N("UI:Main:AddFriend");
    private readonly string rEMOVE_FRIEND = "Remove Friend".L10N("UI:Main:RemoveFriend");
    private readonly string bLOCK = "Block".L10N("UI:Main:Block");
    private readonly string uNBLOCK = "Unblock".L10N("UI:Main:Unblock");
    private readonly string iNVITE = "Invite".L10N("UI:Main:Invite");
    private readonly string jOIN = "Join".L10N("UI:Main:Join");
    private readonly string cOPY_LINK = "Copy Link".L10N("UI:Main:CopyLink");
    private readonly string oPEN_LINK = "Open Link".L10N("UI:Main:OpenLink");

    private readonly CnCNetUserData cncnetUserData;
    private readonly PrivateMessagingWindow pmWindow;
    private XNAContextMenuItem privateMessageItem;
    private XNAContextMenuItem toggleFriendItem;
    private XNAContextMenuItem toggleIgnoreItem;
    private XNAContextMenuItem invitePlayerItem;
    private XNAContextMenuItem joinPlayerItem;
    private XNAContextMenuItem copyLinkItem;
    private XNAContextMenuItem openLinkItem;
    protected GlobalContextMenuData contextMenuData;

    public GlobalContextMenu(
        WindowManager windowManager,
        CnCNetManager connectionManager,
        CnCNetUserData cncnetUserData,
        PrivateMessagingWindow pmWindow)
        : base(windowManager)
    {
        this.connectionManager = connectionManager;
        this.cncnetUserData = cncnetUserData;
        this.pmWindow = pmWindow;

        Name = nameof(GlobalContextMenu);
        ClientRectangle = new Rectangle(0, 0, 150, 2);
        Enabled = false;
        Visible = false;
    }

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
            }, cursorPoint);
    }

    private void Invite()
    {
        // note it's assumed that if the channel name is specified, the game name must be also
        if (string.IsNullOrEmpty(contextMenuData.inviteChannelName) || ProgramConstants.IsInGame)
        {
            return;
        }

        string messageBody = ProgramConstants.GAMEINVITECTCPCOMMAND + " " + contextMenuData.inviteChannelName + ";" + contextMenuData.inviteGameName;

        if (!string.IsNullOrEmpty(contextMenuData.inviteChannelPassword))
        {
            messageBody += ";" + contextMenuData.inviteChannelPassword;
        }

        connectionManager.SendCustomMessage(new QueuedMessage(
            "PRIVMSG " + GetIrcUser().Name + " :\u0001" + messageBody + "\u0001", QueuedMessageType.CHATMESSAGE, 0));
    }

    private void UpdateButtons()
    {
        UpdatePlayerBasedButtons();
        UpdateMessageBasedButtons();
    }

    private void UpdatePlayerBasedButtons()
    {
        IRCUser ircUser = GetIrcUser();
        bool isOnline = ircUser != null && connectionManager.UserList.Any(u => u.Name == ircUser.Name);
        bool isAdmin = contextMenuData.ChannelUser?.IsAdmin ?? false;

        toggleFriendItem.Visible = ircUser != null;
        privateMessageItem.Visible = ircUser != null && isOnline;
        toggleIgnoreItem.Visible = ircUser != null;
        invitePlayerItem.Visible = ircUser != null && isOnline && !string.IsNullOrEmpty(contextMenuData.inviteChannelName);
        joinPlayerItem.Visible = ircUser != null && !contextMenuData.PreventJoinGame && isOnline;

        toggleIgnoreItem.Selectable = !isAdmin;

        if (ircUser == null)
            return;

        toggleFriendItem.Text = cncnetUserData.IsFriend(ircUser.Name) ? rEMOVE_FRIEND : aDD_FRIEND;
        toggleIgnoreItem.Text = cncnetUserData.IsIgnored(ircUser.Ident) ? uNBLOCK : bLOCK;
    }

    private void UpdateMessageBasedButtons()
    {
        string link = contextMenuData?.ChatMessage?.Message?.GetLink();

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

            using Process _ = Process.Start(new ProcessStartInfo
            {
                FileName = link,
                UseShellExecute = true
            });
        };
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
            connectionManager.WhoReplyReceived -= WhoIsReply;
        }

        connectionManager.WhoReplyReceived += WhoIsReply;
        connectionManager.SendWhoIsMessage(ircUser.Name);
    }

    private IRCUser GetIrcUser()
    {
        if (contextMenuData.IrcUser != null)
            return contextMenuData.IrcUser;

        if (contextMenuData.ChannelUser?.IRCUser != null)
            return contextMenuData.ChannelUser.IRCUser;

        if (!string.IsNullOrEmpty(contextMenuData.PlayerName))
            return connectionManager.UserList.Find(u => u.Name == contextMenuData.PlayerName);

        if (!string.IsNullOrEmpty(contextMenuData.ChatMessage?.SenderName))
            return connectionManager.UserList.Find(u => u.Name == contextMenuData.ChatMessage.SenderName);

        return null;
    }

    public void Show(IRCUser ircUser, Point cursorPoint)
    {
        Show(
            new GlobalContextMenuData
            {
                IrcUser = ircUser
            }, cursorPoint);
    }

    public void Show(ChannelUser channelUser, Point cursorPoint)
    {
        Show(
            new GlobalContextMenuData
            {
                ChannelUser = channelUser
            }, cursorPoint);
    }

    public void Show(ChatMessage chatMessage, Point cursorPoint)
    {
        Show(
            new GlobalContextMenuData()
            {
                ChatMessage = chatMessage
            }, cursorPoint);
    }

    public void Show(GlobalContextMenuData data, Point cursorPoint)
    {
        Disable();
        contextMenuData = data;
        UpdateButtons();

        if (!Items.Any(i => i.Visible))
            return;

        Open(cursorPoint);
    }
}