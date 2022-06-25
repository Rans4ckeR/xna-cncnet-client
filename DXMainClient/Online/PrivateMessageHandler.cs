using System;
using DTAClient.Online.EventArguments;

namespace DTAClient.Online;

/// <summary>
/// This is responsible for handling the receiving of private messages from CnCNet and performing
/// any logic checks as to whether the message should be ignored, independent from any GUI. This
/// will then forward valid private message events to other consumers.
/// </summary>
public class PrivateMessageHandler
{
    private readonly CnCNetUserData _cncnetUserData;
    private readonly CnCNetManager _connectionManager;

    private int unreadMessageCount;

    public PrivateMessageHandler(
        CnCNetManager connectionManager,
        CnCNetUserData cncnetUserData)
    {
        _connectionManager = connectionManager;
        _cncnetUserData = cncnetUserData;

        _connectionManager.PrivateMessageReceived += ConnectionManager_PrivateMessageReceived;
    }

    public event EventHandler<PrivateMessageEventArgs> PrivateMessageReceived;

    public event EventHandler<UnreadMessageCountEventArgs> UnreadMessageCountUpdated;

    /// <summary>
    /// This can be called by specific GUI components to trigger than any unread counts should be
    /// incremented, because the PrivateMessageWindow may not currently be visible.
    /// </summary>
    public void IncrementUnreadMessageCount()
        => SetUnreadMessageCount(unreadMessageCount + 1);

    /// <summary>
    /// This can be called by specific GUI components to trigger than any unread counts should be
    /// reset, because the PrivateMessageWindow was made visible.
    /// </summary>
    public void ResetUnreadMessageCount()
        => SetUnreadMessageCount(0);

    private void ConnectionManager_PrivateMessageReceived(object sender, CnCNetPrivateMessageEventArgs e)
    {
        IRCUser iu = _connectionManager.UserList.Find(u => u.Name == e.Sender);

        // We don't accept PMs from people who we don't share any channels with
        if (iu == null)
            return;

        // Messages from users we've blocked are not wanted
        if (_cncnetUserData.IsIgnored(iu.Ident))
            return;

        PrivateMessageEventArgs privateMessageEventArgs = new(e.Sender, e.Message, iu);

        PrivateMessageReceived?.Invoke(this, privateMessageEventArgs);
    }

    private void DoUnreadMessageCountUpdated()
        => UnreadMessageCountUpdated?.Invoke(this, new UnreadMessageCountEventArgs(unreadMessageCount));

    private void SetUnreadMessageCount(int unreadMessageCount)
    {
        this.unreadMessageCount = unreadMessageCount;
        DoUnreadMessageCountUpdated();
    }
}