using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminSpoofUserPingCommand : ChatCommand
    {
        public AdminSpoofUserPingCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var t = Arguments.Count == 0 ? "" : Arguments[0]; // target
            string r; // reply

            if (t.Length == 0 || !Battlenet.Common.GetClientByOnlineName(t, out var target) || target == null)
            {
                r = Resources.UserNotLoggedOn;
                foreach (var line in r.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            Arguments.RemoveAt(0); // remove target
            // Calculates and removes (target+' ') from (raw) which prints into (newRaw):
            RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(t) + (Arguments.Count > 0 ? 1 : 0))..];

            var strPing = Arguments.Count == 0 ? "0" : Arguments[0];
            if (!Daemon.Common.TryToInt32FromString(strPing, out int targetPing))
            {
                r = Resources.AdminSpoofUserPingCommandBadValue;
                foreach (var line in r.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            if (target.ActiveChannel != null)
            {
                target.ActiveChannel.UpdateUser(target, targetPing); // also sets target.Ping = targetPing
            }
            else
            {
                target.Ping = targetPing;
            }

            var targetEnv = new Dictionary<string, string>()
            {
                { "accountName", target.Username },
                { "channel", target.ActiveChannel == null ? "(null)" : target.ActiveChannel.Name },
                { "game", Product.ProductName(target.Product, true) },
                { "host", Settings.GetString(new string[] { "battlenet", "realm", "host" }, "(null)") },
                { "localTime", target.LocalTime.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "name", target.OnlineName },
                { "onlineName", target.OnlineName },
                { "ping", targetPing.ToString() },
                { "realm", Settings.GetString(new string[] { "battlenet", "realm", "name" }, Resources.Battlenet) },
                { "realmTime", DateTime.Now.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "realmTimezone", $"UTC{DateTime.Now:zzz}" },
                { "user", target.OnlineName },
                { "username", target.OnlineName },
                { "userName", target.OnlineName },
            };
            var env = targetEnv.Concat(context.Environment);

            r = Resources.AdminSpoofUserPingCommand;

            foreach (var kv in env)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split(Battlenet.Common.NewLine))
            {
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
            }
        }
    }
}
