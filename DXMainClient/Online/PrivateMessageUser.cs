using System.Collections.Generic;

namespace DTAClient.Online;

internal class PrivateMessageUser
{
    public List<ChatMessage> Messages = new();

    public PrivateMessageUser(IRCUser user)
    {
        IrcUser = user;
    }

    public IRCUser IrcUser { get; private set; }
}