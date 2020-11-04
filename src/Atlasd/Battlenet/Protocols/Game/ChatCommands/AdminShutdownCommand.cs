using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminShutdownCommand : ChatCommand
    {
        public AdminShutdownCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            string periodStr = "0";
            if (Arguments.Count > 0) {
                periodStr = Arguments[0];
                Arguments.RemoveAt(0);
            }

            if (!double.TryParse(periodStr, out var periodDbl))
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, (uint)0, context.GameState.Ping, context.GameState.OnlineName, Resources.AdminShutdownCommandParseError).WriteTo(context.GameState.Client);
                return;
            }

            Battlenet.Common.ScheduleShutdown(TimeSpan.FromSeconds(periodDbl), string.Join(' ', Arguments), context);
        }
    }
}
