using Atlasd.Localization;
using System.Collections.Generic;
using System;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class VersionCommand : ChatCommand
    {
        public VersionCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var assembly = typeof(Program).Assembly;
            var server = $"{assembly.GetName().Name}/{assembly.GetName().Version} ({Program.DistributionMode})";

            var systemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            var systemUptimeStr = $"{Math.Floor(systemUptime.TotalDays)} day{(Math.Floor(systemUptime.TotalDays) == 1 ? "" : "s")} {(systemUptime.Hours < 10 ? "0" : "")}{systemUptime.Hours}:{(systemUptime.Minutes < 10 ? "0" : "")}{systemUptime.Minutes}:{(systemUptime.Seconds < 10 ? "0" : "")}{systemUptime.Seconds}";

            var processUptime = TimeSpan.FromMilliseconds(Environment.TickCount64 - Program.TickCountAtInit);
            var processUptimeStr = $"{Math.Floor(processUptime.TotalDays)} day{(Math.Floor(processUptime.TotalDays) == 1 ? "" : "s")} {(processUptime.Hours < 10 ? "0" : "")}{processUptime.Hours}:{(processUptime.Minutes < 10 ? "0" : "")}{processUptime.Minutes}:{(processUptime.Seconds < 10 ? "0" : "")}{processUptime.Seconds}";

            var hasAdmin = context.GameState.HasAdmin();
            string r = hasAdmin ? Resources.VersionCommandWithAdmin : Resources.VersionCommand;

            context.Environment["version"] = server;
            context.Environment["systemUptime"] = systemUptimeStr;
            context.Environment["processUptime"] = processUptimeStr;

            foreach (var kv in context.Environment)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split(Battlenet.Common.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
