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
                                var friendByteString = Encoding.UTF8.GetBytes(targetString);
                                var friend = new Friend(context.GameState, friendByteString);
                                friends.Add(friend.Username);

                                replyEventId = ChatEvent.EventIds.EID_INFO;
                                reply = Resources.AddedFriend.Replace("{friend}", Encoding.UTF8.GetString(friend.Username));

                                new SID_FRIENDSADD().Invoke(new MessageContext(context.GameState.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>() {{ "friend", friend }}));
                            }
                        }
                        break;
                    }
                case "demote":
                case "d":
                    {
                        var targetString = Arguments.Count > 0 ? Arguments[0] : string.Empty;
                        if (string.IsNullOrEmpty(targetString))
                        {
                            reply = Resources.DemoteFriendEmptyTarget;
                        }
                        else
                        {
                            byte[] exists = null;
                            byte counter1 = 0;
                            byte counter2 = 0;
                            foreach (var friendByteString in friends)
                            {
                                string friendString = Encoding.UTF8.GetString(friendByteString);
                                if (string.Equals(targetString, friendString, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    exists = friendByteString;
                                    break;
                                }
                                counter1++;
                            }
                            if (exists == null || exists.Length == 0)
                            {
                                reply = Resources.DemoteFriendEmptyTarget;
                            }
                            else
                            {
                                if (counter1 == friends.Count - 1)
                                {
                                    counter2 = counter1;
                                }
                                else
                                {
                                    counter2 = (byte)(counter1 + 1);
                                    friends.RemoveAt(counter1);
                                    friends.Insert(counter2, exists);
                                }

                                replyEventId = ChatEvent.EventIds.EID_INFO;
                                reply = Resources.DemotedFriend.Replace("{friend}", Encoding.UTF8.GetString(exists));

                                if (counter1 != counter2)
                                    new SID_FRIENDSPOSITION().Invoke(new MessageContext(context.GameState.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>() {{ "old", counter1 }, { "new", counter2 }}));
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
                            if (friendCount++ == 0) reply += Battlenet.Common.NewLine;
                            var friendString = Encoding.UTF8.GetString(friend);
                            reply += $"{friendCount}: {friendString}{Battlenet.Common.NewLine}";
                        }
                        if (friendCount > 0) reply = reply[0..(reply.Length - Battlenet.Common.NewLine.Length)]; // strip last newline

                        break;
                    }
                case "m":
                case "msg":
                case "message":
                    {
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

                        break;
                    }
                case "promote":
                case "p":
                    {
                        var targetString = Arguments.Count > 0 ? Arguments[0] : string.Empty;
                        if (string.IsNullOrEmpty(targetString))
                        {
                            reply = Resources.PromoteFriendEmptyTarget;
                        }
                        else
                        {
                            byte[] exists = null;
                            byte counter1 = 0;
                            byte counter2 = 0;
                            foreach (var friendByteString in friends)
                            {
                                string friendString = Encoding.UTF8.GetString(friendByteString);
                                if (string.Equals(targetString, friendString, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    exists = friendByteString;
                                    break;
                                }
                                counter1++;
                            }
                            if (exists == null || exists.Length == 0)
                            {
                                reply = Resources.PromoteFriendEmptyTarget;
                            }
                            else
                            {
                                if (counter1 == 0)
                                {
                                    counter2 = counter1;
                                }
                                else
                                {
                                    counter2 = (byte)(counter1 - 1);
                                    friends.RemoveAt(counter1);
                                    friends.Insert(counter2, exists);
                                }

                                replyEventId = ChatEvent.EventIds.EID_INFO;
                                reply = Resources.PromotedFriend.Replace("{friend}", Encoding.UTF8.GetString(exists));

                                if (counter1 != counter2)
                                    new SID_FRIENDSPOSITION().Invoke(new MessageContext(context.GameState.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>() {{ "old", counter1 }, { "new", counter2 }}));
                            }
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
                new ChatEvent(replyEventId, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
            }
        }
    }
}
