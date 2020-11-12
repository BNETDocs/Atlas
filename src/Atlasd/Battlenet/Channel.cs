using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Atlasd.Battlenet
{
    class Channel
    {
        public const Flags TheVoidFlags = Flags.Public | Flags.Silent;

        [Flags]
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

        private Channel(string name, Flags flags = Flags.None, int maxUsers = -1, string topic = "")
        {
            ActiveFlags = flags;
            AllowNewUsers = true;
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

                if (ActiveFlags.HasFlag(Flags.Restricted) || ActiveFlags.HasFlag(Flags.System))
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
                GameState[] users;
                lock (Users) users = Users.ToArray();

                lock (user)
                {
                    foreach (var subuser in users)
                    {
                        // Tell this user about everyone in the channel:
                        new ChatEvent(ChatEvent.EventIds.EID_USERSHOW, subuser.ChannelFlags, subuser.Ping, subuser.OnlineName, Encoding.ASCII.GetString(subuser.Statstring)).WriteTo(user.Client);

                        // Tell everyone else about this user entering the channel:
                        if (subuser != user)
                        {
                            new ChatEvent(ChatEvent.EventIds.EID_USERJOIN, user.ChannelFlags, user.Ping, user.OnlineName, Encoding.ASCII.GetString(user.Statstring)).WriteTo(subuser.Client);
                        }
                    }
                }
            }
            else
            {
                new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, Resources.ChannelIsChatRestricted).WriteTo(user.Client);
            }

            var topic = RenderTopic(user).Split(Environment.NewLine);
            foreach (var line in topic)
                new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line).WriteTo(user.Client);

            if (Common.ScheduledShutdown.EventDate > DateTime.Now)
            {
                var ts = Common.ScheduledShutdown.EventDate - DateTime.Now;
                var tsStr = $"{ts.Hours} hour{(ts.Hours == 1 ? "" : "s")} {ts.Minutes} minute{(ts.Minutes == 1 ? "" : "s")} {ts.Seconds} second{(ts.Seconds == 1 ? "" : "s")}";

                tsStr = tsStr.Replace("0 hours ", "");
                tsStr = tsStr.Replace("0 minutes ", "");
                tsStr = tsStr.Replace(" 0 seconds", "");

                var m = string.IsNullOrEmpty(Common.ScheduledShutdown.AdminMessage) ? Resources.ServerShutdownScheduled : Resources.ServerShutdownScheduledWithMessage;

                m = m.Replace("{message}", Common.ScheduledShutdown.AdminMessage);
                m = m.Replace("{period}", tsStr);

                new ChatEvent(ChatEvent.EventIds.EID_BROADCAST, Account.Flags.Admin, -1, "Battle.net", m).WriteTo(user.Client);
            }

            var autoOp = Settings.GetBoolean(new string[] { "channel", "auto_op" }, false);

            if ((autoOp == true && Count == 1 && IsPrivate()) || Name.ToLower() == "op " + user.OnlineName.ToLower())
                UpdateUser(user, user.ChannelFlags | Account.Flags.ChannelOp);
        }

        public void Dispose()
        {
            if (Users != null)
            {
                var theVoid = GetChannelByName(Resources.TheVoid, true);
                foreach (var user in Users) MoveUser(user, theVoid, true);
            }

            lock (Common.ActiveChannels)
            {
                if (Common.ActiveChannels.ContainsKey(Name))
                    Common.ActiveChannels.Remove(Name);
            }
        }

        public static Channel GetChannelByName(string name, bool autoCreate)
        {
            Channel channel = null;

            if (string.IsNullOrEmpty(name)) return channel;
            if (name[0] == '#') name = name.Substring(1);

            lock (Common.ActiveChannels)
            {
                Common.ActiveChannels.TryGetValue(name, out channel);
            }

            if (channel != null || !autoCreate) return channel;

            var isStatic = GetStaticChannel(name, out var staticName, out var staticFlags, out var staticMaxUsers, out var staticTopic, out var staticProducts);

            if (!isStatic)
            {
                channel = new Channel(name, Flags.None);
            }
            else
            {
                channel = new Channel(staticName, staticFlags, staticMaxUsers, staticTopic);
            }

            lock (Common.ActiveChannels)
            {
                Common.ActiveChannels.Add(channel.Name, channel);
            }

            return channel;
        }

        public static bool GetStaticChannel(string search, out string name, out Flags flags, out int maxUsers, out string topic, out Product.ProductCode[] products)
        {
            var searchL = search.ToLower();

            Settings.State.RootElement.TryGetProperty("channel", out var channelJson);
            channelJson.TryGetProperty("static", out var staticJson);

            foreach (var ch in staticJson.EnumerateArray())
            {
                var hasName = ch.TryGetProperty("name", out var chNameJson);
                var chName = !hasName ? null : chNameJson.GetString();
                if (chName == null || chName.ToLower() != searchL)
                {
                    continue;
                }

                var hasFlags = ch.TryGetProperty("flags", out var chFlagsJson);
                var hasMaxUsers = ch.TryGetProperty("max_users", out var chMaxUsersJson);
                var hasTopic = ch.TryGetProperty("topic", out var chTopicJson);
                var hasProducts = ch.TryGetProperty("products", out var chProductsJson);

                var chFlags = !hasFlags ? Flags.None : (Flags)chFlagsJson.GetUInt32();
                var chMaxUsers = !hasMaxUsers ? -1 : chMaxUsersJson.GetInt32();
                var chTopic = !hasTopic ? "" : chTopicJson.GetString();

                Product.ProductCode[] chProducts = null;
                if (hasProducts)
                {
                    var _list = new List<Product.ProductCode>();
                    foreach (var productJson in chProductsJson.EnumerateArray())
                    {
                        var productStr = productJson.GetString();
                        _list.Add(Product.StringToProduct(productStr));
                    }
                    chProducts = _list.ToArray();
                }

                name = chName;
                flags = chFlags;
                maxUsers = chMaxUsers;
                topic = chTopic;
                products = chProducts;

                return true;
            }

            name = null;
            flags = 0;
            maxUsers = -1;
            topic = null;
            products = new Product.ProductCode[0];

            return false;
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
                    s += $"{n}{Environment.NewLine}";
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

        public void KickUser(GameState source, string target, string reason)
        {
            GameState targetClient = null;

            lock (Users)
            {
                foreach (var client in Users)
                {
                    if (client.OnlineName == target)
                    {
                        targetClient = client;
                        break;
                    }
                }
            }

            if (targetClient == null)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.InvalidUser).WriteTo(source.Client);
                return;
            }

            var sourceSudoPrivs = source.ChannelFlags.HasFlag(Account.Flags.Admin) || source.ChannelFlags.HasFlag(Account.Flags.Employee);
            var targetSudoPrivs = targetClient.ChannelFlags.HasFlag(Account.Flags.Admin)
                || targetClient.ChannelFlags.HasFlag(Account.Flags.ChannelOp)
                || targetClient.ChannelFlags.HasFlag(Account.Flags.Employee);

            if (targetSudoPrivs && !sourceSudoPrivs)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.YouCannotKickAChannelOperator).WriteTo(source.Client);
                return;
            }

            bool maskAdminsInKickMessage = Settings.GetBoolean(new string[] { "battlenet", "emulation", "mask_admins_in_kick_message" }, false);

            var sourceName = source.OnlineName;
            if (maskAdminsInKickMessage)
            {
                if (source.ChannelFlags.HasFlag(Account.Flags.Employee))
                {
                    sourceName = $"a {Resources.BattlenetRepresentative}";
                }
                else if (source.ChannelFlags.HasFlag(Account.Flags.Admin))
                {
                    sourceName = $"a {Resources.BattlenetAdministrator}";
                }
            }

            var kickedStr = reason.Length > 0 ? Resources.UserKickedFromChannelWithReason : Resources.UserKickedFromChannel;

            kickedStr = kickedStr.Replace("{reason}", reason);
            kickedStr = kickedStr.Replace("{source}", sourceName);
            kickedStr = kickedStr.Replace("{target}", target);

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, kickedStr));

            RemoveUser(targetClient);

            kickedStr = Resources.YouWereKickedFromChannel;

            kickedStr = kickedStr.Replace("{reason}", reason);
            kickedStr = kickedStr.Replace("{source}", source.OnlineName);
            kickedStr = kickedStr.Replace("{target}", target);

            new ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, kickedStr).WriteTo(targetClient.Client);

            var theVoid = GetChannelByName(Resources.TheVoid, true);
            MoveUser(targetClient, theVoid, true);
        }

        public static void MoveUser(GameState client, string name, bool ignoreLimits = true)
        {
            var channel = GetChannelByName(name, true);
            MoveUser(client, channel, ignoreLimits);
        }

        public static void MoveUser(GameState client, Channel channel, bool ignoreLimits = true)
        {
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"Moving user [{client.OnlineName}] {(client.ActiveChannel != null ? $"from [{client.ActiveChannel.Name}] " : "")}to [{channel.Name}] (ignoreLimits: {ignoreLimits})");

            channel.AcceptUser(client, ignoreLimits);
        }

        public void RemoveUser(GameState user)
        {
            bool notify = false;
            GameState[] users;

            lock (Users)
            {
                if (Users.Contains(user))
                {
                    Users.Remove(user);
                    notify = true;
                }

                users = Users.ToArray();
            }

            if (notify)
            {
                lock (user)
                {
                    user.ActiveChannel = null;
                    user.ChannelFlags &= ~Account.Flags.ChannelOp;

                    foreach (var subuser in users)
                    {
                        // Tell everyone else about this user leaving the channel:
                        new ChatEvent(ChatEvent.EventIds.EID_USERLEAVE, user.ChannelFlags, user.Ping, user.OnlineName, new byte[0]).WriteTo(subuser.Client);
                    }
                }
            }

            if (Count == 0 && !ActiveFlags.HasFlag(Flags.Public)) Dispose();
        }

        public string RenderTopic(GameState receiver)
        {
            var r = Topic;

            r = r.Replace("{account}", (string)receiver.ActiveAccount.Get(Account.UsernameKey));
            r = r.Replace("{channel}", Name);
            r = r.Replace("{channelMaxUsers}", MaxUsers.ToString());
            r = r.Replace("{channelUserCount}", Users.Count.ToString());
            r = r.Replace("{game}", Product.ProductName(receiver.Product, false));
            r = r.Replace("{gameFull}", Product.ProductName(receiver.Product, true));
            r = r.Replace("{ping}", receiver.Ping.ToString() + "ms");
            r = r.Replace("{user}", receiver.OnlineName);
            r = r.Replace("{username}", receiver.OnlineName);
            r = r.Replace("{userName}", receiver.OnlineName);
            r = r.Replace("{userPing}", receiver.Ping.ToString() + "ms");

            return r;
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
            var oldName = Name;
            Name = newName;
            if (Users.Count == 0) return;

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, $"The channel {oldName} was renamed to {newName}."));
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

        public void SquelchUpdate(GameState client)
        {
            if (client == null) return;

            // TODO
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
                    user.Client.Send(msg.ToByteArray(user.Client.ProtocolType));
                }
            }
        }
    }
}
