using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class TimeCommand : ChatCommand
    {
        public TimeCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var str = Resources.TimeCommand;

            foreach (var kv in context.Environment)
            {
                str = str.Replace("{" + kv.Key + "}", kv.Value);
            }

            str = str.Replace(" 0", "  ");

            foreach (var line in str.Split(Resources.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
