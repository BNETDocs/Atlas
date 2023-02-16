using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminClanCommand : ChatCommand
    {
        public AdminClanCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var cmd = string.Empty;
            if (Arguments.Count > 0)
            {
                cmd = Arguments[0];
                Arguments.RemoveAt(0);
            }

            // Calculates and removes (cmd+' ') from (raw) which prints into (newRaw):
            RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(cmd) + (Arguments.Count > 0 ? 1 : 0))..];

            switch (cmd.ToLower())
            {
                case "list":
                    new AdminClanListCommand(RawBuffer, Arguments).Invoke(context); return;
                default:
                    {
                        var g = context.GameState;
                        var r = Localization.Resources.InvalidAdminCommand;
                        foreach (var kv in context.Environment) r = r.Replace("{" + kv.Key + "}", kv.Value);
                        foreach (var line in r.Split(Battlenet.Common.NewLine))
                            new ChatEvent(ChatEvent.EventIds.EID_ERROR, g.ChannelFlags, g.Client.RemoteIPAddress, g.Ping, g.OnlineName, line).WriteTo(g.Client);
                        break;
                    }
            }
        }
    }
}
