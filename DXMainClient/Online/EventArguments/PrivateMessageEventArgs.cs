namespace DTAClient.Online.EventArguments;

public class PrivateMessageEventArgs : CnCNetPrivateMessageEventArgs
{
    public PrivateMessageEventArgs(string sender, string message, IRCUser ircUser)
        : base(sender, message)
    {
        IrcUser = ircUser;
    }

    public IRCUser IrcUser { get; }
}