using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class InvalidCommand : ChatCommand
    {
        public InvalidCommand(List<string> arguments) : base(arguments) { }

        public new bool CanInvoke(ChatCommandContext context) /* from IChatCommand */
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public new void Invoke(ChatCommandContext context) /* from IChatCommand */
        {
            Channel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, "That is not a valid command. Type /help or /? for more info."), context.GameState.Client);
        }
    }
}
