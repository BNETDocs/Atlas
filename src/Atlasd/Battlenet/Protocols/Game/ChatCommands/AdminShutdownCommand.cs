using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            double.TryParse(periodStr, out var periodDbl);
            var period = TimeSpan.FromSeconds(periodDbl);
            var message = string.Join(' ', Arguments);

            var m = string.IsNullOrEmpty(message) ? Resources.AdminShutdownCommandAnnouncement : Resources.AdminShutdownCommandAnnouncementWithMessage;
            m = m.Replace("{period}", period.ToString());
            m = m.Replace("{message}", message);
            ScheduleShutdown(period, m);

            var r = Resources.AdminShutdownCommandReply;

            foreach (var kv in context.Environment)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split("\r\n"))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }

        private static void ScheduleShutdown(TimeSpan period, string message)
        {
            Task.Run(() =>
            {
                var chatEvent = new ChatEvent(ChatEvent.EventIds.EID_BROADCAST, Account.Flags.Admin, -1, "Battle.net", message);

                lock (Battlenet.Common.ActiveGameStates)
                {
                    foreach (var pair in Battlenet.Common.ActiveGameStates)
                    {
                        chatEvent.WriteTo(pair.Value.Client);
                    }
                }
            });
        }
    }
}
