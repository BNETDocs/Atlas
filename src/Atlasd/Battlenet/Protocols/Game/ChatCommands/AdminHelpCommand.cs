using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminHelpCommand : ChatCommand
    {
        public AdminHelpCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var r = string.Join(Battlenet.Common.NewLine, new List<string>() {
                { Resources.AdminHelpCommand },
                { "/admin ? (alias: /admin help)" },
                { "/admin announce (alias: /admin broadcast)" },
                { "/admin broadcast <message>" },
                { "/admin channel disband [destination]" },
                { "/admin channel flags <integer>" },
                { "/admin channel maxusers <max|-1>" },
                { "/admin channel rename <new name...>" },
                { "/admin channel resync" },
                { "/admin channel topic <new topic...>" },
                { "/admin dc (alias: /admin disconnect)" },
                { "/admin disconnect <user> [reason]" },
                { "/admin help (this text)" },
                { "/admin move (alias: /admin moveuser)" },
                { "/admin moveuser <user> <channel>" },
                { "/admin shutdown [(cancel [message])|(delay-seconds|30 [message])]" },
                { "/admin spoofuserflag (alias: /admin spoofuserflags)" },
                { "/admin spoofuserflags <user> <flags>" },
                { "/admin spoofusergame <user> <game>" },
                { "/admin spoofusername <oldname> <newname>" },
                { "/admin spoofuserping <user> <ping>" },
                { "" },
            });

            foreach (var kv in context.Environment)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split(Battlenet.Common.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
