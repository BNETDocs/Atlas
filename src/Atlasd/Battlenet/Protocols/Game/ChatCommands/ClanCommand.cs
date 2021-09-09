using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class ClanCommand : ChatCommand
    {
        public ClanCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var replyEventId = ChatEvent.EventIds.EID_INFO;
            var reply = string.Empty;
            var subcommand = Arguments.Count == 0 ? "" : Arguments[0];

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
                    || context.GameState.ChannelFlags.HasFlag(Account.Flags.ChannelOp)
                    || userFlags.HasFlag(Account.Flags.Admin)
                    || userFlags.HasFlag(Account.Flags.ChannelOp)
                    || userFlags.HasFlag(Account.Flags.Employee)
                ;
            }

            switch (subcommand.ToLower())
            {
                case "public":
                case "pub":
                    {
                        if (!hasSudo || context.GameState.ActiveChannel == null)
                        {
                            replyEventId = ChatEvent.EventIds.EID_ERROR;
                            reply = Resources.YouAreNotAChannelOperator;
                        }
                        else
                        {
                            context.GameState.ActiveChannel.SetAllowNewUsers(true);
                        }
                        break;
                    }
                case "private":
                case "priv":
                    {
                        if (!hasSudo || context.GameState.ActiveChannel == null)
                        {
                            replyEventId = ChatEvent.EventIds.EID_ERROR;
                            reply = Resources.YouAreNotAChannelOperator;
                        }
                        else
                        {
                            context.GameState.ActiveChannel.SetAllowNewUsers(false);
                        }
                        break;
                    }
                default:
                    {
                        replyEventId = ChatEvent.EventIds.EID_ERROR;
                        reply = Resources.InvalidChatCommand;
                        break;
                    }
            }

            if (string.IsNullOrEmpty(reply)) return;

            foreach (var kv in context.Environment)
            {
                reply = reply.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in reply.Split(Battlenet.Common.NewLine))
            {
                new ChatEvent(replyEventId, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
            }
        }
    }
}
