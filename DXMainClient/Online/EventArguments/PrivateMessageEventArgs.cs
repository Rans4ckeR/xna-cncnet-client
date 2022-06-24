namespace DTAClient.Online.EventArguments;

public class PrivateMessageEventArgs : CnCNetPrivateMessageEventArgs
{
    public readonly IRCUser IrcUser;

    public PrivateMessageEventArgs(string sender, string message, IRCUser ircUser)
        : base(sender, message)
    {
        this.IrcUser = ircUser;
    }
}