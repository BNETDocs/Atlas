using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class JoinCommand : ChatCommand
    {
        public JoinCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            if (Arguments.Count < 1)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, (uint)0, context.GameState.Ping, context.GameState.OnlineName, Resources.InvalidChannelName).WriteTo(context.GameState.Client);
                return;
            }

            var channelName = string.Join(" ", Arguments);

            context.GameState.ActiveAccount.Get(Account.FlagsKey, out var userFlags);
            var ignoreLimits = ((Account.Flags)((AccountKeyValue)userFlags).Value).HasFlag(Account.Flags.Employee);

            var channel = Channel.GetChannelByName(channelName, true);
            channel.AcceptUser(context.GameState, ignoreLimits, false);
        }
    }
}
