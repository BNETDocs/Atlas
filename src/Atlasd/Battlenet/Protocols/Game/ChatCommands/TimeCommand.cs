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

            str = str.Replace("{realm}", "BNETDocs");
            str = str.Replace("{realmTime}", DateTime.Now.ToString(Common.HumanDateTimeFormat));
            str = str.Replace("{localTime}", context.GameState.LocalTime.ToString(Common.HumanDateTimeFormat));

            foreach (var line in str.Split("\r\n"))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
