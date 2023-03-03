using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminCommand : ChatCommand
    {
        public AdminCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            if (!context.GameState.HasAdmin())
            {
                new InvalidCommand(RawBuffer, Arguments).Invoke(context);
                return;
            }

            var cmd = string.Empty;
            if (Arguments.Count > 0)
            {
                cmd = Arguments[0];
                Arguments.RemoveAt(0);
            }

            // Calculates and removes (cmd+' ') from (raw) which prints into (newRaw):
            RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(cmd) + (Arguments.Count > 0 ? 1 : 0))..];

            switch (cmd.ToLowerInvariant())
            {
                case "announce":
                case "broadcast":
                    new AdminBroadcastCommand(RawBuffer, Arguments).Invoke(context); return;
                case "channel":
                case "chan":
                case "ch":
                    new AdminChannelCommand(RawBuffer, Arguments).Invoke(context); return;
                case "clan":
                    new AdminClanCommand(RawBuffer, Arguments).Invoke(context); return;
                case "disconnect":
                case "dc":
                    new AdminDisconnectCommand(RawBuffer, Arguments).Invoke(context); return;
                case "disconnectforflooding":
                case "disconnectforflood":
                case "dcforflooding":
                case "dcforflood":
                    new AdminDisconnectForFloodingCommand(RawBuffer, Arguments).Invoke(context); return;
                case "help":
                case "?":
                    new AdminHelpCommand(RawBuffer, Arguments).Invoke(context); return;
                case "messagebox":
                case "msgbox":
                case "msg":
                    new AdminMessageBoxCommand(RawBuffer, Arguments).Invoke(context); return;
                case "moveuser":
                case "move":
                    new AdminMoveUserCommand(RawBuffer, Arguments).Invoke(context); return;
                case "reload":
                    new AdminReloadCommand(RawBuffer, Arguments).Invoke(context); return;
                case "shutdown":
                    new AdminShutdownCommand(RawBuffer, Arguments).Invoke(context); return;
                case "spoofuserflag":
                case "spoofuserflags":
                    new AdminSpoofUserFlagsCommand(RawBuffer, Arguments).Invoke(context); return;
                case "spoofusergame":
                    new AdminSpoofUserGameCommand(RawBuffer, Arguments).Invoke(context); return;
                case "spoofusername":
                    new AdminSpoofUserNameCommand(RawBuffer, Arguments).Invoke(context); return;
                case "spoofuserping":
                    new AdminSpoofUserPingCommand(RawBuffer, Arguments).Invoke(context); return;
                case "spoofuserstatstring":
                    new AdminSpoofUserStatstringCommand(RawBuffer, Arguments).Invoke(context); return;
                default:
                    {
                        var r = Localization.Resources.InvalidAdminCommand;
                        foreach (var kv in context.Environment) r = r.Replace("{" + kv.Key + "}", kv.Value);
                        foreach (var line in r.Split(Battlenet.Common.NewLine))
                            new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                        break;
                    }
            }
        }
    }
}
