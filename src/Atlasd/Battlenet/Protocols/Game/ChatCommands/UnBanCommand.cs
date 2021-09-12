using Atlasd.Localization;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class UnBanCommand : ChatCommand
    {
        public UnBanCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            if (context.GameState.ActiveChannel == null)
            {
                new InvalidCommand(RawBuffer, Arguments).Invoke(context);
                return;
            }

            if (!(context.GameState.ChannelFlags.HasFlag(Account.Flags.Employee)
                || context.GameState.ChannelFlags.HasFlag(Account.Flags.ChannelOp)
                || context.GameState.ChannelFlags.HasFlag(Account.Flags.Admin)))
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, Resources.YouAreNotAChannelOperator).WriteTo(context.GameState.Client);
                return;
            }

            var target = "";
            if (Arguments.Count > 0)
            {
                target = Arguments[0];
                Arguments.RemoveAt(0);
                RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(target) + (Arguments.Count > 0 ? 1 : 0))..];
            }

            context.GameState.ActiveChannel.UnBanUser(context.GameState, target);
        }
    }
}
