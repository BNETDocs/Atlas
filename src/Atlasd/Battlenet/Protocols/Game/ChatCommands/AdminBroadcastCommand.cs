using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminBroadcastCommand : ChatCommand
    {
        public AdminBroadcastCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var text = string.Join(' ', Arguments);

            Task.Run(() =>
            {
                var chatEvent = new ChatEvent(ChatEvent.EventIds.EID_BROADCAST, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, text);

                lock (Battlenet.Common.ActiveGameStates)
                {
                    foreach (var pair in Battlenet.Common.ActiveGameStates)
                    {
                        chatEvent.WriteTo(pair.Value.Client);
                    }
                }
            });
        }
    }
}
