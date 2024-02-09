using System;

namespace DTAClient.Domain.Multiplayer.LAN
{
    internal sealed class ServerNoParamCommandHandler : LANServerCommandHandler
    {
        public ServerNoParamCommandHandler(string commandName, Action<LANPlayerInfo> handler)
            : base(commandName)
        {
            this.handler = handler;
        }

        private Action<LANPlayerInfo> handler;

        public override bool Handle(LANPlayerInfo pInfo, string message)
        {
            if (string.Equals(message, CommandName, StringComparison.OrdinalIgnoreCase))
            {
                handler(pInfo);
                return true;
            }

            return false;
        }
    }
}