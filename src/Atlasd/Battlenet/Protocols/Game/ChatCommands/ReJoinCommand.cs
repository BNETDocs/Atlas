using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class ReJoinCommand : ChatCommand
    {
        public ReJoinCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

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
                return;
            }

            Channel.MoveUser(context.GameState, context.GameState.ActiveChannel, true);
        }
    }
}
