using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class FriendAddCommand : ChatCommand
    {
        public FriendAddCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var replyEventId = ChatEvent.EventIds.EID_ERROR;
            var reply = string.Empty;
            var friends = (List<byte[]>)context.GameState.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());

            var targetString = Arguments.Count > 0 ? Arguments[0] : string.Empty;
            if (string.IsNullOrEmpty(targetString))
            {
                reply = Resources.AddFriendEmptyTarget;
            }
            else
            {
                var exists = false;
                foreach (var friendByteString in friends)
                {
                    string friendString = Encoding.UTF8.GetString(friendByteString);
                    if (string.Equals(targetString, friendString, StringComparison.CurrentCultureIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists)
                {
                    reply = Resources.AlreadyAddedFriend.Replace("{friend}", targetString);
                }
                else
                {
                    var friendByteString = Encoding.UTF8.GetBytes(targetString);
                    var friend = new Friend(context.GameState, friendByteString);
                    friends.Add(friend.Username);

                    replyEventId = ChatEvent.EventIds.EID_INFO;
                    reply = Resources.AddedFriend.Replace("{friend}", Encoding.UTF8.GetString(friend.Username));

                    new SID_FRIENDSADD().Invoke(new MessageContext(context.GameState.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>() {{ "friend", friend }}));
                }
            }

            if (string.IsNullOrEmpty(reply)) return;
            foreach (var kv in context.Environment) reply = reply.Replace("{" + kv.Key + "}", kv.Value);
            foreach (var line in reply.Split(Battlenet.Common.NewLine))
                new ChatEvent(replyEventId, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
