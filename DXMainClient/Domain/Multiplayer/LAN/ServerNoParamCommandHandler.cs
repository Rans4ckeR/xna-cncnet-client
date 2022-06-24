using System;

namespace DTAClient.Domain.Multiplayer.LAN;

public class ServerNoParamCommandHandler : LANServerCommandHandler
{
    private readonly Action<LANPlayerInfo> handler;

    public ServerNoParamCommandHandler(
        string commandName,
        Action<LANPlayerInfo> handler)
        : base(commandName)
    {
        this.handler = handler;
    }

    public override bool Handle(LANPlayerInfo pInfo, string message)
    {
        if (message == CommandName)
        {
            handler(pInfo);
            return true;
        }

        return false;
    }
}