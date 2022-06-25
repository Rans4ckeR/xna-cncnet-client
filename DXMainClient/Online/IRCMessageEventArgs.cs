using System;

namespace DTAClient.Online;

public class IRCMessageEventArgs : EventArgs
{
    public IRCMessageEventArgs(ChatMessage ircMessage)
    {
        Message = ircMessage;
    }

    public ChatMessage Message { get; private set; }
}