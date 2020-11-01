using Atlasd.Localization;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class KickCommand : ChatCommand
    {
        public KickCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            if (context.GameState.ActiveChannel == null)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, (uint)0, context.GameState.Ping, context.GameState.OnlineName, Resources.InvalidChatCommand).WriteTo(context.GameState.Client);
                return;
            }

            if (!(context.GameState.ChannelFlags.HasFlag(Account.Flags.Admin)
                || context.GameState.ChannelFlags.HasFlag(Account.Flags.ChannelOp)
                || context.GameState.ChannelFlags.HasFlag(Account.Flags.Employee)))
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, (uint)0, context.GameState.Ping, context.GameState.OnlineName, Resources.YouAreNotAChannelOperator).WriteTo(context.GameState.Client);
                return;
            }

            if (Arguments.Count < 1)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, (uint)0, context.GameState.Ping, context.GameState.OnlineName, Resources.UserNotLoggedOn).WriteTo(context.GameState.Client);
                return;
            }

            var target = Arguments[0];
            Arguments.RemoveAt(0);

            var reason = string.Join(" ", Arguments);

            context.GameState.ActiveChannel.KickUser(context.GameState, target, reason);
        }
    }
}
