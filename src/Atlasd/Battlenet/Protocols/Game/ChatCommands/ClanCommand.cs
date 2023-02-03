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
            var hasAdmin = context.GameState.HasAdmin(true); // includeChannelOp=true
            var replyEventId = ChatEvent.EventIds.EID_ERROR;
            var reply = string.Empty;

            if (!hasAdmin || context.GameState.ActiveChannel == null)
            {
                reply = Resources.YouAreNotAChannelOperator;
            }
            else
            {
                var subcommand = Arguments.Count > 0 ? Arguments[0] : string.Empty;
                if (!string.IsNullOrEmpty(subcommand)) Arguments.RemoveAt(0);

                switch (subcommand.ToLower())
                {
                    case "motd":
                        {
                            context.GameState.ActiveChannel.SetTopic(string.Join(" ", Arguments));
                            break;
                        }
                    case "public":
                    case "pub":
                        {
                            context.GameState.ActiveChannel.SetAllowNewUsers(true);
                            break;
                        }
                    case "private":
                    case "priv":
                        {
                            context.GameState.ActiveChannel.SetAllowNewUsers(false);
                            break;
                        }
                    default:
                        {
                            reply = Resources.InvalidChatCommand;
                            break;
                        }
                }
            }

            if (string.IsNullOrEmpty(reply)) return;

            foreach (var kv in context.Environment)
            {
                reply = reply.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in reply.Split(Battlenet.Common.NewLine))
            {
                new ChatEvent(replyEventId, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
            }
        }
    }
}
