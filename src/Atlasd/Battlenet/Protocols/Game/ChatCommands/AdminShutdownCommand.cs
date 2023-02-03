using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminShutdownCommand : ChatCommand
    {
        public AdminShutdownCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var periodStr = Arguments.Count == 0 ? "" : Arguments[0];
            if (Arguments.Count > 0) Arguments.RemoveAt(0);
            var message = string.Join(' ', Arguments);
            if (message.Length == 0) message = null;

            if (periodStr.Length == 0)
            {
                periodStr = "30"; // default 30 seconds delay if empty periodStr
            }

            if (periodStr.Equals("cancel"))
            {
                Battlenet.Common.ScheduleShutdownCancelled(message, context);
            }
            else
            {
                if (!double.TryParse(periodStr, out var periodDbl))
                {
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, Resources.AdminShutdownCommandParseError).WriteTo(context.GameState.Client);
                    return;
                }

                Battlenet.Common.ScheduleShutdown(TimeSpan.FromSeconds(periodDbl), message, context);
            }
        }
    }
}
