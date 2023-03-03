using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class FriendRemoveCommand : ChatCommand
    {
        public FriendRemoveCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

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
                reply = Resources.RemoveFriendEmptyTarget;
            }
            else
            {
                byte[] exists = null;
                byte counter = 0;
                foreach (var friendByteString in friends)
                {
                    string friendString = Encoding.UTF8.GetString(friendByteString);
                    if (string.Equals(targetString, friendString, StringComparison.CurrentCultureIgnoreCase))
                    {
                        exists = friendByteString;
                        break;
                    }
                    counter++;
                }
                if (exists == null || exists.Length == 0)
                {
                    reply = Resources.AlreadyRemovedFriend.Replace("{friend}", targetString);
                }
                else
                {
                    replyEventId = ChatEvent.EventIds.EID_INFO;
                    reply = Resources.RemovedFriend.Replace("{friend}", targetString);

                    friends.Remove(exists);

                    new SID_FRIENDSREMOVE().Invoke(new MessageContext(context.GameState.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>() {{ "friend", counter }}));
                }
            }

            if (string.IsNullOrEmpty(reply)) return;
            foreach (var kv in context.Environment) reply = reply.Replace("{" + kv.Key + "}", kv.Value);
            foreach (var line in reply.Split(Battlenet.Common.NewLine))
                new ChatEvent(replyEventId, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
