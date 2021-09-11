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
            var hasAdmin = HasAdmin(context.GameState, true); // includeChannelOp=true

            switch (subcommand.ToLower())
            {
                case "public":
                case "pub":
                    {
                        if (!hasAdmin || context.GameState.ActiveChannel == null)
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
                        if (!hasAdmin || context.GameState.ActiveChannel == null)
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
