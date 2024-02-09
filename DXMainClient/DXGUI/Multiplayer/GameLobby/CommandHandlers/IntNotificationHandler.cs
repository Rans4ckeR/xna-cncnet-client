using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers
{
    public class IntNotificationHandler : CommandHandlerBase
    {
        public IntNotificationHandler(string commandName, Action<string, int, Action<int>> action,
            Action<int> innerAction) : base(commandName)
        {
            this.action = action;
            this.innerAction = innerAction;
        }

        Action<string, int, Action<int>> action;
        Action<int> innerAction;

        public override bool Handle(string sender, string message)
        {
            if (message.StartsWith(CommandName, StringComparison.OrdinalIgnoreCase))
            {
                string intPart = message[(CommandName.Length + 1)..];
                bool success = int.TryParse(intPart, out int value);

                action(sender, value, innerAction);
                return true;
            }

            return false;
        }
    }
}