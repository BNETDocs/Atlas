using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AwayCommand : ChatCommand
    {
        public AwayCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            string message = Arguments.Count == 0 ? null : string.Join(" ", Arguments);
            string r;

            if (context.GameState.Away == null || (message != null && message.Length > 0))
            {
                if (message == null || message.Length == 0) message = "Not available";
                context.GameState.Away = message;
                r = Resources.AwayCommandOn;
            } else
            {
                context.GameState.Away = null;
                r = Resources.AwayCommandOff;
            }

            foreach (var kv in context.Environment)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split("\r\n"))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
