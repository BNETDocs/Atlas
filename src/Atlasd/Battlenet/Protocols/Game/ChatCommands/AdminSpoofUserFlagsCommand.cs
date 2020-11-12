using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminSpoofUserFlagsCommand : ChatCommand
    {
        public AdminSpoofUserFlagsCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var t = Arguments.Count == 0 ? "" : Arguments[0];
            string r;

            if (t.Length == 0 || !Battlenet.Common.GetClientByOnlineName(t, out var target) || target == null)
            {
                r = Resources.UserNotLoggedOn;
                foreach (var line in r.Split(Resources.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            Arguments.RemoveAt(0); // remove t
            
            if (target.ActiveChannel == null)
            {
                r = Resources.UserNotInChannel;
                foreach (var line in r.Split(Resources.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            var strFlags = Arguments.Count == 0 ? "0" : Arguments[0];
            if (!Daemon.Common.TryToUInt32FromString(strFlags, out uint targetFlags))
            {
                r = Resources.AdminSpoofUserFlagsCommandBadNumber;
                foreach (var line in r.Split(Resources.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            target.ChannelFlags = (Account.Flags)targetFlags;
            target.ActiveChannel.UpdateUser(target, (Account.Flags)targetFlags);

            var targetEnv = new Dictionary<string, string>()
            {
                { "accountName", target.Username },
                { "channel", target.ActiveChannel == null ? "(null)" : target.ActiveChannel.Name },
                { "flags", $"0x{targetFlags:X8}" },
                { "game", Product.ProductName(target.Product, true) },
                { "host", "BNETDocs" },
                { "localTime", target.LocalTime.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "name", target.OnlineName },
                { "onlineName", target.OnlineName },
                { "realm", "BNETDocs" },
                { "realmTime", DateTime.Now.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "realmTimezone", $"UTC{DateTime.Now:zzz}" },
                { "user", target.OnlineName },
                { "username", target.OnlineName },
                { "userName", target.OnlineName },
            };
            var env = targetEnv.Concat(context.Environment);

            r = Resources.AdminSpoofUserFlagsCommand;

            foreach (var kv in env)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split(Resources.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
