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
            var grantSudoToSpoofedAdmins = Settings.GetBoolean(new string[] { "battlenet", "emulation", "grant_sudo_to_spoofed_admins" }, false);
            var hasSudo = false;

            lock (context.GameState)
            {
                var userFlags = (Account.Flags)context.GameState.ActiveAccount.Get(Account.FlagsKey);
                hasSudo =
                    (
                        grantSudoToSpoofedAdmins && (
                            context.GameState.ChannelFlags.HasFlag(Account.Flags.Admin)
                            || context.GameState.ChannelFlags.HasFlag(Account.Flags.Employee)
                        )
                    )
                    || userFlags.HasFlag(Account.Flags.Admin)
                    || userFlags.HasFlag(Account.Flags.Employee)
                ;
            }

            if (!hasSudo)
            {
                new InvalidCommand(RawBuffer, Arguments).Invoke(context);
                return;
            }

            string cmd;

            if (Arguments.Count == 0)
            {
                cmd = "";
            }
            else
            {
                cmd = Arguments[0];
                Arguments.RemoveAt(0);
            }

            // Calculates and removes (cmd+' ') from (raw) which prints into (newRaw):
            RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(cmd) + (Arguments.Count > 0 ? 1 : 0))..];

            string r;

            switch (cmd.ToLower())
            {
                case "announce":
                case "broadcast":
                    new AdminBroadcastCommand(RawBuffer, Arguments).Invoke(context); return;
                case "channel":
                case "chan":
                    new AdminChannelCommand(RawBuffer, Arguments).Invoke(context); return;
                case "disconnect":
                case "dc":
                    new AdminDisconnectCommand(RawBuffer, Arguments).Invoke(context); return;
                case "help":
                case "?":
                    new AdminHelpCommand(RawBuffer, Arguments).Invoke(context); return;
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
                default:
                    r = Localization.Resources.InvalidAdminCommand;
                    break;
            }

            foreach (var kv in context.Environment)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split(Battlenet.Common.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
