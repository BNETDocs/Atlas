using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class HelpCommand : ChatCommand
    {
        public HelpCommand(List<string> arguments) : base(arguments) { }

        public new bool CanInvoke(ChatCommandContext context) /* from IChatCommand */
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public new void Invoke(ChatCommandContext context) /* from IChatCommand */
        {
            var lines = new List<string>();

            lines.Add("Battle.net help topics:");
            lines.Add("TODO");

            foreach (var line in lines)
                Channel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line), context.GameState.Client);
        }
    }
}
