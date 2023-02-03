using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminSpoofUserGameCommand : ChatCommand
    {
        public AdminSpoofUserGameCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

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

            var strGame = Arguments.Count == 0 ? "" : Arguments[0];
            var targetGame = Product.StringToProduct(strGame);
            if (targetGame == Product.ProductCode.None)
            {
                r = Resources.AdminSpoofUserGameCommandBadValue;
                foreach (var line in r.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            lock (target.Statstring)
            {
                var newStatstring = new byte[target.Statstring.Length];

                using var m = new MemoryStream(newStatstring);
                using var w = new BinaryWriter(m);

                w.Write((UInt32)targetGame);
                w.Write(target.Statstring[4..]);

                if (target.ActiveChannel != null)
                {
                    target.ActiveChannel.UpdateUser(target, newStatstring); // also sets target.Statstring = newStatstring
                }
                else
                {
                    target.Statstring = newStatstring;
                }
            }

            var targetEnv = new Dictionary<string, string>()
            {
                { "accountName", target.Username },
                { "channel", target.ActiveChannel == null ? "(null)" : target.ActiveChannel.Name },
                { "game", Product.ProductName(targetGame, true) },
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

            r = Resources.AdminSpoofUserGameCommand;

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
