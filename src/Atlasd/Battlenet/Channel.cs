using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atlasd.Battlenet
{
    class Channel
    {
        public enum Flags : UInt32
        {
            None            = 0x00000, // aka "Private"
            Public          = 0x00001,
            Moderated       = 0x00002,
            Restricted      = 0x00004,
            Silent          = 0x00008,
            System          = 0x00010,
            ProductSpecific = 0x00020,
            Global          = 0x01000,
            Redirected      = 0x04000,
            Chat            = 0x08000,
            TechSupport     = 0x10000,
        };

        public Flags ActiveFlags { get; protected set; }
        public bool AllowNewUsers { get; protected set; } 
        protected List<GameState> BannedUsers { get; private set; }
        public int Count { get => Users.Count; }
        public int MaxUsers { get; protected set; }
        public string Name { get; protected set; }
        public string Topic { get; protected set; }
        protected List<GameState> Users { get; private set; }
    
        public Channel(string name, Flags flags, int maxUsers = -1, string topic = "")
        {
            Common.ActiveChannels.Add(name, this);

            ActiveFlags = flags;
            BannedUsers = new List<GameState>();
            MaxUsers = maxUsers;
            Name = name;
            Topic = topic;
            Users = new List<GameState>();
        }

        public void AcceptUser(GameState user, bool ignoreLimits = false, bool noCreate = false)
        {
            if (!ignoreLimits)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Channel, string.Format("[{0}] Evaluating limits for user [{1}]", Name, user.OnlineName));

                if (MaxUsers > -1 && Users.Count >= MaxUsers)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, string.Format("[{0}] Rejecting user [{1}] for reason [{2}]", Name, user.OnlineName, "full"));

                    if (noCreate)
                        WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_CHANNELFULL, ActiveFlags, 0, "", Name), user.Client);
                    else
                        WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, "That channel is full."), user.Client);

                    return;
                }

                lock (BannedUsers)
                {
                    if (BannedUsers.Contains(user))
                    {
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, string.Format("[{0}] Rejecting user [{1}] for reason [{2}]", Name, user.OnlineName, "banned"));

                        if (noCreate)
                            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_CHANNELRESTRICTED, ActiveFlags, 0, "", Name), user.Client);
                        else
                            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, "You are banned from that channel."));

                        return;
                    }
                }

                if (ActiveFlags.HasFlag(Flags.Restricted))
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, string.Format("[{0}] Rejecting user [{1}] for reason [{2}]", Name, user.OnlineName, "restricted"));

                    if (noCreate)
                        WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_CHANNELRESTRICTED, ActiveFlags, 0, "", Name), user.Client);
                    else
                        WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, "That channel is restricted."), user.Client);

                    return;
                }
            }

            if (user.ActiveChannel != null) user.ActiveChannel.RemoveUser(user);
            user.ActiveChannel = this;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, string.Format("[{0}] Accepting user [{1}] (ignoreLimits: {2})", Name, user.OnlineName, ignoreLimits));

            // Add this user to the channel:
            lock (Users) Users.Add(user);

            // Tell this user they entered the channel:
            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_CHANNELJOIN, ActiveFlags, 0, "", Name), user.Client);

            if (!ActiveFlags.HasFlag(Flags.Silent))
            {
                lock (user)
                {
                    lock (Users)
                    {
                        foreach (var subuser in Users)
                        {
                            // Tell this user about everyone in the channel:
                            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_USERSHOW, (UInt32)subuser.ChannelFlags, subuser.Ping, subuser.OnlineName, Product.ProductToStatstring(subuser.Product)), user.Client, subuser.Client);

                            // Tell everyone else about this user entering the channel:
                            if (subuser != user)
                                WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_USERJOIN, (UInt32)user.ChannelFlags, user.Ping, user.OnlineName, Product.ProductToStatstring(user.Product)), subuser.Client, user.Client);
                        }
                    }
                }
            }
            else
            {
                WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, "This channel does not have chat privileges."), user.Client);
            }

            var topic = RenderTopic(user).Split("\n");
            foreach (var line in topic)
                WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line), user.Client);

            if (Name.ToLower() == "op " + user.OnlineName.ToLower())
                UpdateUser(user, user.ChannelFlags | Account.Flags.ChannelOp);
        }

        public void Dispose()
        {
            if (Users != null)
            {
                var theVoid = Channel.GetChannelByName("The Void");
                if (theVoid == null) theVoid = new Channel("The Void", Flags.Public | Flags.Silent, -1);
                foreach (var user in Users) Channel.MoveUser(user, theVoid);
            }

            if (Common.ActiveChannels.ContainsKey(Name))
                Common.ActiveChannels.Remove(Name);
        }

        public static Channel GetChannelByName(string name)
        {
            return Common.ActiveChannels.TryGetValue(name, out Channel channel) ? channel : null;
        }

        public bool IsPrivate()
        {
            return !ActiveFlags.HasFlag(Channel.Flags.Public);
        }

        public bool IsPublic()
        {
            return ActiveFlags.HasFlag(Channel.Flags.Public);
        }

        public static void MoveUser(GameState client, string name, bool ignoreLimits = true)
        {
            var channel = Channel.GetChannelByName(name);
            if (channel == null) channel = new Channel(name, 0);
            MoveUser(client, channel, ignoreLimits);
        }

        public static void MoveUser(GameState client, Channel channel, bool ignoreLimits = true)
        {
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, "Moving user [" + client.OnlineName + "] " + (client.ActiveChannel != null ? "from [" + client.ActiveChannel.Name + "] " : "") + "to [" + channel.Name + "] (ignoreLimits: " + (ignoreLimits ? "yes" : "no") + ")");

            channel.AcceptUser(client, ignoreLimits);
        }

        public void RemoveUser(GameState user)
        {
            lock (Users)
            {
                if (!Users.Contains(user)) return;

                Users.Remove(user);

                foreach (var subuser in Users)
                {
                    // Tell everyone else about this user leaving the channel:
                    WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_USERLEAVE, user.ChannelFlags, user.Ping, user.OnlineName, Encoding.ASCII.GetString(user.Statstring)), subuser.Client, user.Client);
                }
            }

            lock (user)
            {
                user.ChannelFlags &= ~Account.Flags.ChannelOp;
            }

            if (Count == 0 && !ActiveFlags.HasFlag(Flags.Public)) Dispose();
        }

        public string RenderTopic(GameState receiver)
        {
            var rendered_topic = Topic;

            rendered_topic.Replace("%channel.name%", Name);
            rendered_topic.Replace("%channel.maxusers%", MaxUsers.ToString());
            rendered_topic.Replace("%channel.users%", Users.Count.ToString());
            rendered_topic.Replace("%user.account%", (string)receiver.ActiveAccount.Get(Account.UsernameKey));
            rendered_topic.Replace("%user.game%", Product.ProductName(receiver.Product, false));
            rendered_topic.Replace("%user.gamelong%", Product.ProductName(receiver.Product, true));
            rendered_topic.Replace("%user.name%", receiver.OnlineName);
            rendered_topic.Replace("%user.ping%", receiver.Ping.ToString() + "ms");

            return rendered_topic;
        }

        public void Resync()
        {
            lock (Users)
            {
                foreach (var user in Users)
                {
                    // Tell users they re-entered the channel:
                    WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_CHANNELJOIN, ActiveFlags, 0, "", Name), user.Client);

                    // Show users in channel or display info about no chat:
                    if (!ActiveFlags.HasFlag(Flags.Silent))
                    {
                        var chatEvent = new ChatEvent(ChatEvent.EventIds.EID_USERSHOW, user.ChannelFlags, user.Ping, user.OnlineName, Encoding.ASCII.GetString(user.Statstring));
                        foreach (var subuser in Users) WriteChatEvent(chatEvent, subuser.Client, user.Client);
                    }
                    else
                    {
                        WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, "This channel does not have chat privileges."), user.Client);
                    }

                    // Channel topic:
                    var topic = RenderTopic(user);
                    foreach (var line in topic.Split("\n"))
                        WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line), user.Client);
                }
            }
        }

        public void SetActiveFlags(Flags newFlags)
        {
            ActiveFlags = newFlags;
            Resync();
        }

        public void SetAllowNewUsers(bool allowNewUsers)
        {
            AllowNewUsers = allowNewUsers;

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, "This channel is now " + (AllowNewUsers ? "public, new users are allowed" : "private, new users are rejected") + "."));
        }

        public void SetMaxUsers(int maxUsers)
        {
            MaxUsers = maxUsers;

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, String.Format("The upper user limit for this channel was changed to {0:D}.", MaxUsers)));
        }

        public void SetName(string newName)
        {
            Name = newName;

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, "The channel was renamed."));

            Resync();
        }

        public void SetTopic(string newTopic)
        {
            Topic = newTopic;

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, "The channel topic was changed to:"));

            lock (Users)
            {
                foreach (var user in Users)
                {
                    var lines = RenderTopic(user).Split("\n");

                    foreach (var line in lines)
                        WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line), user.Client);
                }
            }
        }

        // This function should only be called if any of the attributes were modified outside of this class.
        public void UpdateUser(GameState client)
        {
            UpdateUser(client, client.ChannelFlags, client.Ping, Encoding.ASCII.GetString(client.Statstring));
        }

        public void UpdateUser(GameState client, Account.Flags flags)
        {
            UpdateUser(client, flags, client.Ping, Encoding.ASCII.GetString(client.Statstring));
        }

        public void UpdateUser(GameState client, Int32 ping)
        {
            UpdateUser(client, client.ChannelFlags, ping, Encoding.ASCII.GetString(client.Statstring));
        }

        public void UpdateUser(GameState client, byte[] statstring)
        {
            UpdateUser(client, client.ChannelFlags, client.Ping, Encoding.ASCII.GetString(statstring));
        }

        public void UpdateUser(GameState client, string statstring)
        {
            UpdateUser(client, client.ChannelFlags, client.Ping, statstring);
        }

        public void UpdateUser(GameState client, Account.Flags flags, Int32 ping, string statstring)
        {
            client.ChannelFlags = flags;
            client.Ping = ping;
            client.Statstring = Encoding.ASCII.GetBytes(statstring);

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_USERUPDATE, client.ChannelFlags, client.Ping, client.OnlineName, Encoding.ASCII.GetString(client.Statstring)));
        }

        public static void WriteChatEvent(ChatEvent chatEvent, ClientState receiver)
        {
            var args = new Dictionary<string, object> { { "chatEvent", chatEvent } };
            var msg = new SID_CHATEVENT();

            msg.Invoke(new MessageContext(receiver, Protocols.MessageDirection.ServerToClient, args));

            switch (receiver.ProtocolType)
            {
                case ProtocolType.Game:
                    {
                        receiver.Send(msg.ToByteArray());
                        break;
                    }
                case ProtocolType.Chat:
                case ProtocolType.Chat_Alt1:
                case ProtocolType.Chat_Alt2:
                    {
                        receiver.Send(Encoding.ASCII.GetBytes(msg.ToString()));
                        break;
                    }
                default:
                    {
                        throw new NotSupportedException("Invalid channel state, user in channel is using an incompatible protocol");
                    }
            }
        }

        public void WriteChatEvent(ChatEvent chatEvent, ClientState receiver = null, ClientState sender = null)
        {
            if (receiver == sender && chatEvent.EventId == ChatEvent.EventIds.EID_TALK)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Channel, "[" + Name + "] Dropping EID_TALK from being echoed to sender");
                return;
            }

            var args = new Dictionary<string, object> {{ "chatEvent", chatEvent }};
            var msg = new SID_CHATEVENT();

            lock (Users)
            {
                foreach (var user in Users)
                {
                    msg.Invoke(new MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args));

                    switch (user.Client.ProtocolType)
                    {
                        case ProtocolType.Game:
                            {
                                user.Client.Send(msg.ToByteArray());
                                break;
                            }
                        case ProtocolType.Chat:
                        case ProtocolType.Chat_Alt1:
                        case ProtocolType.Chat_Alt2:
                            {
                                user.Client.Send(Encoding.ASCII.GetBytes(msg.ToString()));
                                break;
                            }
                        default:
                            {
                                throw new NotSupportedException("Invalid channel state, user in channel is using an incompatible protocol");
                            }
                    }
                }
            }
        }
    }
}
