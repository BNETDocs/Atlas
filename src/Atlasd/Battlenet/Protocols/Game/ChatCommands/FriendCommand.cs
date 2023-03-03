using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class FriendCommand : ChatCommand
    {
        public FriendCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var replyEventId = ChatEvent.EventIds.EID_ERROR;
            var reply = string.Empty;

            var subcommand = Arguments.Count > 0 ? Arguments[0] : string.Empty;
            if (!string.IsNullOrEmpty(subcommand))
            {
                Arguments.RemoveAt(0);
                // Calculates and removes (subcmd+' ') from (RawBuffer) which prints into (RawBuffer):
                var stripSize = subcommand.Length + (RawBuffer.Length - subcommand.Length > 0 ? 1 : 0);
                RawBuffer = RawBuffer[stripSize..];
            }

            var friends = (List<byte[]>)context.GameState.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());

            switch (subcommand.ToLowerInvariant())
            {
                case "add":
                case "a":
                    new FriendAddCommand(RawBuffer, Arguments).Invoke(context); break;
                case "demote":
                case "d":
                    new FriendDemoteCommand(RawBuffer, Arguments).Invoke(context); break;
                case "list":
                case "l":
                    new FriendListCommand(RawBuffer, Arguments).Invoke(context); break;
                case "message":
                case "msg":
                case "m":
                    new FriendMessageCommand(RawBuffer, Arguments).Invoke(context); break;
                case "promote":
                case "p":
                    new FriendPromoteCommand(RawBuffer, Arguments).Invoke(context); break;
                case "remove":
                case "rem":
                case "r":
                    new FriendRemoveCommand(RawBuffer, Arguments).Invoke(context); break;
                default:
                    {
                        reply = Resources.InvalidChatCommand;
                        break;
                    }
            }

            if (string.IsNullOrEmpty(reply)) return;
            foreach (var kv in context.Environment) reply = reply.Replace("{" + kv.Key + "}", kv.Value);
            foreach (var line in reply.Split(Battlenet.Common.NewLine))
                new ChatEvent(replyEventId, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
