using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet
{
    class Channel
    {
        public const string TheVoid = "The Void";
        public const Flags TheVoidFlags = Flags.Public | Flags.Silent;

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

        public void AcceptUser(GameState user, bool ignoreLimits = false, bool extendedErrors = false)
        {
            if (!ignoreLimits)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Channel, $"[{Name}] Evaluating limits for user [{user.OnlineName}]");

                if (MaxUsers > -1 && Users.Count >= MaxUsers)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"[{Name}] Rejecting user [{user.OnlineName}] for reason [Full]");

                    if (extendedErrors)
                        new ChatEvent(ChatEvent.EventIds.EID_CHANNELFULL, ActiveFlags, 0, "", Name).WriteTo(user.Client);
                    else
                        new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.ChannelIsFull).WriteTo(user.Client);

                    return;
                }

                lock (BannedUsers)
                {
                    if (BannedUsers.Contains(user))
                    {
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"[{Name}] Rejecting user [{user.OnlineName}] for reason [Banned]");

                        if (extendedErrors)
                            new ChatEvent(ChatEvent.EventIds.EID_CHANNELRESTRICTED, ActiveFlags, 0, "", Name).WriteTo(user.Client);
                        else
                            new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.YouAreBannedFromThatChannel).WriteTo(user.Client);

                        return;
                    }
                }

                if (ActiveFlags.HasFlag(Flags.Restricted))
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"[{Name}] Rejecting user [{user.OnlineName}] for reason [Restricted]");

                    if (extendedErrors)
                        new ChatEvent(ChatEvent.EventIds.EID_CHANNELRESTRICTED, ActiveFlags, 0, "", Name).WriteTo(user.Client);
                    else
                        new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.ChannelIsRestricted).WriteTo(user.Client);

                    return;
                }
            }

            if (user.ActiveChannel != null) user.ActiveChannel.RemoveUser(user);
            user.ActiveChannel = this;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, string.Format("[{0}] Accepting user [{1}] (ignoreLimits: {2})", Name, user.OnlineName, ignoreLimits));

            // Add this user to the channel:
            lock (Users) Users.Add(user);

            // Tell this user they entered the channel:
            new ChatEvent(ChatEvent.EventIds.EID_CHANNELJOIN, ActiveFlags, 0, "", Name).WriteTo(user.Client);

            if (!ActiveFlags.HasFlag(Flags.Silent))
            {
                lock (user)
                {
                    lock (Users)
                    {
                        foreach (var subuser in Users)
                        {
                            // Tell this user about everyone in the channel:
                            new ChatEvent(ChatEvent.EventIds.EID_USERSHOW, subuser.ChannelFlags, subuser.Ping, subuser.OnlineName, Product.ProductToStatstring(subuser.Product)).WriteTo(user.Client);

                            // Tell everyone else about this user entering the channel:
                            if (subuser != user)
                                new ChatEvent(ChatEvent.EventIds.EID_USERJOIN, user.ChannelFlags, user.Ping, user.OnlineName, Product.ProductToStatstring(user.Product)).WriteTo(subuser.Client);
                        }
                    }
                }
            }
            else
            {
                new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, "This channel does not have chat privileges.").WriteTo(user.Client);
            }

            var topic = RenderTopic(user).Split("\n");
            foreach (var line in topic)
                new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line).WriteTo(user.Client);

            var autoOp = false;
            lock (Daemon.Common.Settings)
            {
                Daemon.Common.Settings.TryGetValue("channel.auto_op", out object _autoOp);
                autoOp = (bool)_autoOp;
            }

            if ((autoOp == true && Count == 1 && IsPrivate()) || Name.ToLower() == "op " + user.OnlineName.ToLower())
                UpdateUser(user, user.ChannelFlags | Account.Flags.ChannelOp);
        }

        public void Dispose()
        {
            if (Users != null)
            {
                var theVoid = GetChannelByName(TheVoid);
                if (theVoid == null) theVoid = new Channel(TheVoid, TheVoidFlags, -1);
                foreach (var user in Users) MoveUser(user, theVoid);
            }

            if (Common.ActiveChannels.ContainsKey(Name))
                Common.ActiveChannels.Remove(Name);
        }

        public static Channel GetChannelByName(string name)
        {
            return Common.ActiveChannels.TryGetValue(name, out Channel channel) ? channel : null;
        }

        public string GetUsersAsString()
        {
            if (ActiveFlags.HasFlag(Flags.Silent)) return "";

            var names = new LinkedList<string>();

            lock (Users)
            {
                foreach (var user in Users)
                {
                    if (user.ChannelFlags.HasFlag(Account.Flags.Employee) ||
                        user.ChannelFlags.HasFlag(Account.Flags.ChannelOp) ||
                        user.ChannelFlags.HasFlag(Account.Flags.Admin))
                    {
                        names.AddFirst($"[{user.OnlineName.ToUpper()}]");
                    } else
                    {
                        names.AddLast(user.OnlineName);
                    }
                }
            }

            var s = "";
            var i = 0;
            foreach (var n in names)
            {
                if (i % 2 == 0)
                {
                    s += $"{n}, ";
                } else
                {
                    s += $"{n}\r\n";
                }
                i++;
            }
            if (i % 2 != 0) s = s[0..^2]; // trim trailing comma

            return s;
        }

        public bool IsPrivate()
        {
            return !ActiveFlags.HasFlag(Flags.Public);
        }

        public bool IsPublic()
        {
            return ActiveFlags.HasFlag(Flags.Public);
        }

        public static void MoveUser(GameState client, string name, bool ignoreLimits = true)
        {
            var channel = GetChannelByName(name);
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
            lock (user)
            {
                lock (Users)
                {
                    if (!Users.Contains(user)) return;

                    Users.Remove(user);
                    user.ActiveChannel = null;

                    foreach (var subuser in Users)
                    {
                        // Tell everyone else about this user leaving the channel:
                        new ChatEvent(ChatEvent.EventIds.EID_USERLEAVE, user.ChannelFlags, user.Ping, user.OnlineName, Encoding.ASCII.GetString(user.Statstring)).WriteTo(subuser.Client);
                    }
                }

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
                    WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_CHANNELJOIN, ActiveFlags, 0, "", Name), user.Client.GameState);

                    // Show users in channel or display info about no chat:
                    if (!ActiveFlags.HasFlag(Flags.Silent))
                    {
                        var chatEvent = new ChatEvent(ChatEvent.EventIds.EID_USERSHOW, user.ChannelFlags, user.Ping, user.OnlineName, Encoding.ASCII.GetString(user.Statstring));
                        foreach (var subuser in Users) WriteChatEvent(chatEvent, subuser.Client.GameState);
                    }
                    else
                    {
                        WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, Resources.ChannelIsChatRestricted), user.Client.GameState);
                    }

                    // Channel topic:
                    var topic = RenderTopic(user);
                    foreach (var line in topic.Split("\n"))
                        WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line), user.Client.GameState);
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
                        new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line).WriteTo(user.Client);
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

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_USERUPDATE, client.ChannelFlags, client.Ping, client.OnlineName, Encoding.ASCII.GetString(client.Statstring)), client);
        }

        public void WriteChatEvent(ChatEvent chatEvent, GameState owner = null)
        {
            var args = new Dictionary<string, object> {{ "chatEvent", chatEvent }};
            var msg = new SID_CHATEVENT();

            lock (Users)
            {
                foreach (var user in Users)
                {
                    if (owner != null && user == owner && chatEvent.EventId == ChatEvent.EventIds.EID_TALK)
                    {
                        // Dropping EID_TALK from being echoed back to sender
                        continue;
                    }

                    msg.Invoke(new MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args));
                    user.Client.Send(msg.ToByteArray());
                }
            }
        }

        public static void WriteServerStats(ClientState receiver)
        {
            var chatEvents = GetServerStats(receiver);

            foreach (var chatEvent in chatEvents)
                chatEvent.WriteTo(receiver);
        }

        public static List<ChatEvent> GetServerStats(ClientState receiver)
        {
            var chatEvents = new List<ChatEvent>();

            if (receiver == null || receiver.GameState == null || receiver.GameState.ActiveChannel == null)
                return chatEvents;

            var channel = receiver.GameState.ActiveChannel;

            var strGame = Product.ProductName(receiver.GameState.Product, true);
            var numGameOnline = Common.GetActiveClientCountByProduct(receiver.GameState.Product);
            var numGameAdvertisements = 0;
            var numTotalOnline = Common.ActiveClients.Count;
            var numTotalAdvertisements = 0;

            var str = Resources.ChannelFirstJoinGreeting;

            str = str.Replace("{realm}", "Battle.net");
            str = str.Replace("{host}", "BNETDocs");
            str = str.Replace("{game}", strGame);
            str = str.Replace("{gameUsers}", numGameOnline.ToString("#,0"));
            str = str.Replace("{gameAds}", numGameAdvertisements.ToString("#,0"));
            str = str.Replace("{totalUsers}", numTotalOnline.ToString("#,0"));
            str = str.Replace("{totalGameAds}", numTotalAdvertisements.ToString("#,0"));

            foreach (var line in str.Split("\r\n"))
                chatEvents.Add(new ChatEvent(ChatEvent.EventIds.EID_INFO, channel.ActiveFlags, 0, channel.Name, line));

            return chatEvents;
        }
    }
}
