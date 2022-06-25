using System;
using System.Collections.Generic;
using ClientCore;
using DTAClient.DXGUI;
using DTAClient.Online.EventArguments;
using Localization;

namespace DTAClient.Online;

public class Channel : IMessageView
{
    private const int MESSAGE_LIMIT = 1024;

    private readonly Connection connection;

    private string _topic;

    private bool notifyOnUserListChange = true;

    public Channel(string uiName, string channelName, bool persistent, bool isChatChannel, string password, Connection connection)
    {
        Users = isChatChannel
            ? new SortedUserCollection<ChannelUser>(ChannelUser.ChannelUserComparison)
            : new UnsortedUserCollection<ChannelUser>();

        UIName = uiName;
        ChannelName = channelName.ToLowerInvariant();
        Persistent = persistent;
        IsChatChannel = isChatChannel;
        Password = password;
        this.connection = connection;

        if (persistent)
        {
            Instance_SettingsSaved(null, EventArgs.Empty);
            UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;
        }
    }

    /// <summary>
    /// Raised when the server informs the client that it's is unable to join the channel because
    /// it's full.
    /// </summary>
    public event EventHandler ChannelFull;

    public event EventHandler<ChannelModeEventArgs> ChannelModesChanged;

    public event EventHandler<ChannelCTCPEventArgs> CTCPReceived;

    public event EventHandler InvalidPasswordEntered;

    public event EventHandler InviteOnlyErrorOnJoin;

    public event EventHandler<IRCMessageEventArgs> MessageAdded;

    /// <summary>
    /// Raised when the server informs the client that it's is unable to join the channel because
    /// the client has attempted to join too many channels too quickly.
    /// </summary>
    public event EventHandler<MessageEventArgs> TargetChangeTooFast;

    public event EventHandler<ChannelUserEventArgs> UserAdded;

    public event EventHandler<ChannelUserEventArgs> UserGameIndexUpdated;

    public event EventHandler<UserNameEventArgs> UserKicked;

    public event EventHandler<UserNameEventArgs> UserLeft;

    public event EventHandler UserListCleared;

    public event EventHandler UserListReceived;

    public event EventHandler<UserNameChangedEventArgs> UserNameChanged;

    public event EventHandler<UserNameEventArgs> UserQuitIRC;

    #region Public members

    public string ChannelName { get; }

    public bool IsChatChannel { get; }

    public string Password { get; private set; }

    public bool Persistent { get; }

    public string UIName { get; }

    #endregion Public members

    public List<ChatMessage> Messages { get; } = new();

    public string Topic
    {
        get
        {
            return _topic;
        }

        set
        {
            _topic = value;
            if (Persistent)
            {
                AddMessage(new ChatMessage(
                    string.Format("Topic for {0} is: {1}".L10N("UI:Main:ChannelTopic"), UIName, _topic)));
            }
        }
    }

    public IUserCollection<ChannelUser> Users { get; private set; }

    public void AddMessage(ChatMessage message)
    {
        if (Messages.Count == MESSAGE_LIMIT)
            Messages.RemoveAt(0);

        Messages.Add(message);

        MessageAdded?.Invoke(this, new IRCMessageEventArgs(message));
    }

    public void AddUser(ChannelUser user)
    {
        Users.Add(user.IRCUser.Name, user);
        UserAdded?.Invoke(this, new ChannelUserEventArgs(user));
    }

    public void ClearUsers()
    {
        Users.Clear();
        UserListCleared?.Invoke(this, EventArgs.Empty);
    }

    public void Join()
    {
        // Wait a random amount of time before joining to prevent join/part floods
        if (Persistent)
        {
            int rn = connection.Rng.Next(1, 10000);

            if (string.IsNullOrEmpty(Password))
                connection.QueueMessage(QueuedMessageType.SYSTEMMESSAGE, 9, rn, "JOIN " + ChannelName);
            else
                connection.QueueMessage(QueuedMessageType.SYSTEMMESSAGE, 9, rn, "JOIN " + ChannelName + " " + Password);
        }
        else
        {
            if (string.IsNullOrEmpty(Password))
                connection.QueueMessage(QueuedMessageType.SYSTEMMESSAGE, 9, "JOIN " + ChannelName);
            else
                connection.QueueMessage(QueuedMessageType.SYSTEMMESSAGE, 9, "JOIN " + ChannelName + " " + Password);
        }
    }

    public void Leave()
    {
        // Wait a random amount of time before joining to prevent join/part floods
        if (Persistent)
        {
            int rn = connection.Rng.Next(1, 10000);
            connection.QueueMessage(QueuedMessageType.SYSTEMMESSAGE, 9, rn, "PART " + ChannelName);
        }
        else
        {
            connection.QueueMessage(QueuedMessageType.SYSTEMMESSAGE, 9, "PART " + ChannelName);
        }

        ClearUsers();
    }

    public void OnChannelFull()
    {
        ChannelFull?.Invoke(this, EventArgs.Empty);
    }

    public void OnChannelModesChanged(string sender, string modes)
    {
        ChannelModesChanged?.Invoke(this, new ChannelModeEventArgs(sender, modes));
    }

    public void OnCTCPReceived(string userName, string message)
    {
        CTCPReceived?.Invoke(this, new ChannelCTCPEventArgs(userName, message));
    }

    public void OnInvalidJoinPassword()
    {
        InvalidPasswordEntered?.Invoke(this, EventArgs.Empty);
    }

    public void OnInviteOnlyOnJoin()
    {
        InviteOnlyErrorOnJoin?.Invoke(this, EventArgs.Empty);
    }

    public void OnTargetChangeTooFast(string message)
    {
        TargetChangeTooFast?.Invoke(this, new MessageEventArgs(message));
    }

    public void OnUserJoined(ChannelUser user)
    {
        AddUser(user);

        if (notifyOnUserListChange)
        {
            AddMessage(new ChatMessage(
                string.Format("{0} has joined {1}.".L10N("UI:Main:PlayerJoinChannel"), user.IRCUser.Name, UIName)));
        }

#if !YR
        if (Persistent && IsChatChannel && user.IRCUser.Name == ProgramConstants.PLAYERNAME)
            RequestUserInfo();
#endif
    }

    public void OnUserKicked(string userName)
    {
        if (Users.Remove(userName))
        {
            if (userName == ProgramConstants.PLAYERNAME)
            {
                Users.Clear();
            }

            AddMessage(new ChatMessage(
                string.Format("{0} has been kicked from {1}.".L10N("UI:Main:PlayerKickedFromChannel"), userName, UIName)));

            UserKicked?.Invoke(this, new UserNameEventArgs(userName));
        }
    }

    public void OnUserLeft(string userName)
    {
        if (Users.Remove(userName))
        {
            if (notifyOnUserListChange)
            {
                AddMessage(new ChatMessage(
                     string.Format("{0} has left from {1}.".L10N("UI:Main:PlayerLeftFromChannel"), userName, UIName)));
            }

            UserLeft?.Invoke(this, new UserNameEventArgs(userName));
        }
    }

    public void OnUserListReceived(List<ChannelUser> userList)
    {
        for (int i = 0; i < userList.Count; i++)
        {
            ChannelUser user = userList[i];
            ChannelUser existingUser = Users.Find(user.IRCUser.Name);
            if (existingUser == null)
            {
                Users.Add(user.IRCUser.Name, user);
            }
            else if (IsChatChannel)
            {
                if (existingUser.IsAdmin != user.IsAdmin)
                {
                    existingUser.IsAdmin = user.IsAdmin;
                    existingUser.IsFriend = user.IsFriend;
                    Users.Reinsert(user.IRCUser.Name);
                }
            }
        }

        UserListReceived?.Invoke(this, EventArgs.Empty);
    }

    public void OnUserNameChanged(string oldUserName, string newUserName)
    {
        ChannelUser user = Users.Find(oldUserName);
        if (user != null)
        {
            _ = Users.Remove(oldUserName);
            Users.Add(newUserName, user);
            UserNameChanged?.Invoke(this, new UserNameChangedEventArgs(oldUserName, user.IRCUser));
        }
    }

    public void OnUserQuitIRC(string userName)
    {
        if (Users.Remove(userName))
        {
            if (notifyOnUserListChange)
            {
                AddMessage(new ChatMessage(
                    string.Format("{0} has quit from CnCNet.".L10N("UI:Main:PlayerQuitCncNet"), userName)));
            }

            UserQuitIRC?.Invoke(this, new UserNameEventArgs(userName));
        }
    }

    public void RequestUserInfo()
    {
        connection.QueueMessage(QueuedMessageType.SYSTEMMESSAGE, 9, "WHO " + ChannelName);
    }

    /// <summary>
    /// Sends a "ban host" message to the channel.
    /// </summary>
    /// <param name="host">The host that should be banned.</param>
    /// <param name="priority">The priority of the message in the send queue.</param>
    public void SendBanMessage(string host, int priority)
    {
        connection.QueueMessage(
            QueuedMessageType.INSTANTMESSAGE,
            priority,
            string.Format("MODE {0} +b *!*@{1}", ChannelName, host));
    }

    public void SendChatMessage(string message, IRCColor color)
    {
        AddMessage(new ChatMessage(ProgramConstants.PLAYERNAME, color.XnaColor, DateTime.Now, message));

        string colorString = ((char)03).ToString() + color.IrcColorId.ToString("D2");

        connection.QueueMessage(
            QueuedMessageType.CHATMESSAGE,
            0,
            "PRIVMSG " + ChannelName + " :" + colorString + message);
    }

    /// <summary>
    /// SendCTCPMessage.
    /// </summary>
    /// <param name="message">message.</param>
    /// <param name="qmType">qmType.</param>
    /// <param name="priority">priority.</param>
    /// <param name="replace">
    /// This can be used to help prevent flooding for multiple options that are changed quickly. It
    /// allows for a single message for multiple changes.
    /// </param>
    public void SendCTCPMessage(string message, QueuedMessageType qmType, int priority, bool replace = false)
    {
        char cTCPChar1 = (char)58;
        char cTCPChar2 = (char)01;

        connection.QueueMessage(
            qmType,
            priority,
            "NOTICE " + ChannelName + " " + cTCPChar1 + cTCPChar2 + message + cTCPChar2,
            replace);
    }

    /// <summary>
    /// Sends a "kick user" message to the channel.
    /// </summary>
    /// <param name="userName">The name of the user that should be kicked.</param>
    /// <param name="priority">The priority of the message in the send queue.</param>
    public void SendKickMessage(string userName, int priority)
    {
        connection.QueueMessage(QueuedMessageType.INSTANTMESSAGE, priority, "KICK " + ChannelName + " " + userName);
    }

    public void UpdateGameIndexForUser(string userName)
    {
        ChannelUser user = Users.Find(userName);
        if (user != null)
            UserGameIndexUpdated?.Invoke(this, new ChannelUserEventArgs(user));
    }

    private void Instance_SettingsSaved(object sender, EventArgs e)
    {
#if YR
        notifyOnUserListChange = false;
#else
        notifyOnUserListChange = UserINISettings.Instance.NotifyOnUserListChange;
#endif
    }
}