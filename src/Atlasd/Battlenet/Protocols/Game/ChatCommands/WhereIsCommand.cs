using Atlasd.Localization;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class WhereIsCommand : ChatCommand
    {
        public WhereIsCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var t = Arguments.Count == 0 ? "" : Arguments[0]; // target
            string r; // reply

            // quick check
            if (t.ToLower() == context.GameState.OnlineName.ToLower())
            {
                new WhoAmICommand(RawBuffer, Arguments).Invoke(context);
                return;
            }

            if (!Battlenet.Common.GetClientByOnlineName(t, out var target) || target == null)
            {
                r = Resources.UserNotLoggedOn;
                foreach (var line in r.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            if (target == context.GameState)
            {
                new WhoAmICommand(RawBuffer, Arguments).Invoke(context);
                return;
            }

            var ch = target.ActiveChannel;
            var str = ch == null ? Resources.UserIsUsingGameInRealm : Resources.UserIsUsingGameInTheChannel;

            if (target.Away != null)
            {
                str += Battlenet.Common.NewLine + Resources.AwayCommandStatus.Replace("{awayMessage}", target.Away);
            }

            var targetEnv = new Dictionary<string, string>()
            {
                { "accountName", target.Username },
                { "channel", target.ActiveChannel == null ? "(null)" : target.ActiveChannel.Name },
                { "game", Product.ProductName(target.Product, true) },
                { "host", "BNETDocs" },
                { "localTime", target.LocalTime.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "name", Channel.RenderOnlineName(context.GameState, target) },
                { "onlineName", Channel.RenderOnlineName(context.GameState, target) },
                { "realm", "BNETDocs" },
                { "realmTime", DateTime.Now.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "realmTimezone", $"UTC{DateTime.Now:zzz}" },
                { "user", Channel.RenderOnlineName(context.GameState, target) },
                { "username", Channel.RenderOnlineName(context.GameState, target) },
                { "userName", Channel.RenderOnlineName(context.GameState, target) },
            };
            var env = targetEnv.Concat(context.Environment);

            foreach (var kv in env)
            {
                str = str.Replace("{" + kv.Key + "}", kv.Value);
            }

            new ChatEvent(ChatEvent.EventIds.EID_INFO, Channel.RenderChannelFlags(context.GameState, target), target.Ping, Channel.RenderOnlineName(context.GameState, target), str).WriteTo(context.GameState.Client);
        }
    }
}
