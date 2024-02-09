using System;

namespace DTAClient.Domain.Multiplayer.LAN
{
    /// <summary>
    /// A command handler that has no parameters.
    /// </summary>
    class ClientNoParamCommandHandler : LANClientCommandHandler
    {
        public ClientNoParamCommandHandler(string commandName, Action commandHandler) : base(commandName)
        {
            this.commandHandler = commandHandler;
        }

        Action commandHandler;

        public override bool Handle(string message)
        {
            if (!string.Equals(message, CommandName, StringComparison.OrdinalIgnoreCase))
                return false;

            commandHandler();
            return true;
        }
    }
}