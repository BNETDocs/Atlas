using Atlasd.Localization;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class WhoAmICommand : ChatCommand
    {
        public WhoAmICommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var ch = context.GameState.ActiveChannel;
            var str = ch == null ? Resources.YouAreUsingGameInRealm : Resources.YouAreUsingGameInTheChannel;

            str = str.Replace("{name}", context.GameState.OnlineName);
            str = str.Replace("{game}", Product.ProductName(context.GameState.Product, true));
            str = str.Replace("{realm}", "BNETDocs");
            str = str.Replace("{channel}", ch == null ? "(null)" : ch.Name);

            new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, str).WriteTo(context.GameState.Client);
        }
    }
}
