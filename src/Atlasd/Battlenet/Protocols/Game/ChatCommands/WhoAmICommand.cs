using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class WhoAmICommand : ChatCommand
    {
        public WhoAmICommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var ch = context.GameState.ActiveChannel;
            var r = ch == null ? Resources.YouAreUsingGameInRealm : Resources.YouAreUsingGameInTheChannel;

            if (context.GameState.Away != null)
            {
                r += Battlenet.Common.NewLine + Resources.AwayCommandStatusSelf.Replace("{awayMessage}", context.GameState.Away);
            }

            r = r.Replace("{channel}", ch == null ? "(null)" : ch.Name);
            r = r.Replace("{realm}", "BNETDocs");

            foreach (var kv in context.Environment)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, r).WriteTo(context.GameState.Client);
        }
    }
}
