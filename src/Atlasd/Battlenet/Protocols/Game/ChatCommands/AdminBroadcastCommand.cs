using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminBroadcastCommand : ChatCommand
    {
        public AdminBroadcastCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            Task.Run(() =>
            {
                var chatEvent = new ChatEvent(ChatEvent.EventIds.EID_BROADCAST, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, RawBuffer);

                foreach (var gameState in Battlenet.Common.ActiveGameStates.Values)
                {
                    chatEvent.WriteTo(gameState.Client);
                }
            });
        }
    }
}
