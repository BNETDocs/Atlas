using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class FriendMessageCommand : ChatCommand
    {
        public FriendMessageCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var replyEventId = ChatEvent.EventIds.EID_ERROR;
            var reply = string.Empty;
            var friends = (List<byte[]>)context.GameState.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());

            var messageString = string.Join(" ", Arguments);
            if (string.IsNullOrEmpty(messageString))
            {
                reply = Resources.WhisperCommandEmptyMessage;
            }
            else
            {
                new ChatEvent(ChatEvent.EventIds.EID_WHISPERTO, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, Resources.WhisperFromYourFriends, messageString).WriteTo(context.GameState.Client);

                foreach (var friend in friends)
                {
                    var friendString = Encoding.UTF8.GetString(friend);
                    if (!Battlenet.Common.GetClientByOnlineName(friendString, out var friendGameState) || friendGameState == null) continue;
                    if (!string.IsNullOrEmpty(friendGameState.DoNotDisturb)) continue;
                    new ChatEvent(ChatEvent.EventIds.EID_WHISPERFROM, context.GameState.ChannelFlags, context.GameState.Ping, Channel.RenderOnlineName(friendGameState, context.GameState), messageString).WriteTo(friendGameState.Client);
                }
            }

            if (string.IsNullOrEmpty(reply)) return;
            foreach (var kv in context.Environment) reply = reply.Replace("{" + kv.Key + "}", kv.Value);
            foreach (var line in reply.Split(Battlenet.Common.NewLine))
                new ChatEvent(replyEventId, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
