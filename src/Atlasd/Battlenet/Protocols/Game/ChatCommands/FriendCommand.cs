using Atlasd.Daemon;
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
            if (!string.IsNullOrEmpty(subcommand)) Arguments.RemoveAt(0);

            var friends = (List<byte[]>)context.GameState.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());

            switch (subcommand.ToLower())
            {
                case "add":
                case "a":
                    {
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
                                replyEventId = ChatEvent.EventIds.EID_INFO;
                                reply = Resources.AddedFriend.Replace("{friend}", targetString);
                                friends.Add(Encoding.UTF8.GetBytes(targetString));
                            }
                        }
                        break;
                    }
                case "list":
                case "l":
                    {
                        replyEventId = ChatEvent.EventIds.EID_INFO;
                        reply = Resources.YourFriendsList;

                        var friendCount = 0;
                        foreach (var friend in friends)
                        {
                            if (friendCount++ == 0) reply += Environment.NewLine;
                            var friendString = Encoding.UTF8.GetString(friend);
                            reply += $"{friendCount}: {friendString}{Environment.NewLine}";
                        }

                        break;
                    }
                case "remove":
                case "rem":
                case "r":
                    {
                        var targetString = Arguments.Count > 0 ? Arguments[0] : string.Empty;
                        if (string.IsNullOrEmpty(targetString))
                        {
                            reply = Resources.RemoveFriendEmptyTarget;
                        }
                        else
                        {
                            byte[] exists = null;
                            foreach (var friendByteString in friends)
                            {
                                string friendString = Encoding.UTF8.GetString(friendByteString);
                                if (string.Equals(targetString, friendString, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    exists = friendByteString;
                                    break;
                                }
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
                            }
                        }
                        break;
                    }
                default:
                    {
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
