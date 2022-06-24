using System;

namespace DTAClient.Domain.Multiplayer.LAN;

public class ClientStringCommandHandler : LANClientCommandHandler
{
    private readonly Action<string> action;

    public ClientStringCommandHandler(string commandName, Action<string> action)
        : base(commandName)
    {
        this.action = action;
    }

    public override bool Handle(string message)
    {
        if (!message.StartsWith(CommandName))
            return false;

        action(message.Substring(CommandName.Length + 1));
        return true;
    }
}