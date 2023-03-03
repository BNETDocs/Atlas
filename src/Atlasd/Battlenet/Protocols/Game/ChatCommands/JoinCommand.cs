using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class JoinCommand : ChatCommand
    {
        public JoinCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var gs = context.GameState;
            if (gs.ActiveChannel == null)
            {
                new InvalidCommand(RawBuffer, Arguments).Invoke(context);
                return;
            }

            if (Arguments.Count < 1)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, gs.ChannelFlags, gs.Client.RemoteIPAddress, gs.Ping, gs.OnlineName, Resources.InvalidChannelName).WriteTo(gs.Client);
                return;
            }

            var channelName = string.Join(" ", Arguments);

            gs.ActiveAccount.Get(Account.FlagsKey, out var userFlags);
            var ignoreLimits = ((Account.Flags)((AccountKeyValue)userFlags).Value).HasFlag(Account.Flags.Employee);

            if (StringComparer.InvariantCultureIgnoreCase.Equals(channelName, gs.ActiveChannel.Name)) return;
            Channel.MoveUser(gs, channelName, true, ignoreLimits, false);
        }
    }
}
