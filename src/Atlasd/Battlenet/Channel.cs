﻿using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Atlasd.Battlenet
{
    class Channel
    {
        public const Flags TheVoidFlags = Flags.Public | Flags.Silent;

        [Flags]
        public enum Flags : UInt32
        {
            None = 0x00000, // aka "Private"
            Public = 0x00001,
            Moderated = 0x00002,
            Restricted = 0x00004,
            Silent = 0x00008,
            System = 0x00010,
            ProductSpecific = 0x00020,
            Global = 0x01000,
            Redirected = 0x04000,
            Chat = 0x08000,
            TechSupport = 0x10000,
        };

        public Flags ActiveFlags { get; protected set; }
        public bool AllowNewUsers { get; protected set; }
        protected List<GameState> BannedUsers { get; private set; }
        public int Count { get => Users != null ? Users.Count : 0; }
        public Dictionary<GameState, GameState> DesignatedHeirs { get; protected set; }
        public bool IsClosed { get; private set; }
        private object IsClosing = new object();
        public int MaxUsers { get; protected set; }
        public string Name { get; protected set; }
        public string Topic { get; protected set; }
        protected List<GameState> Users { get; private set; }

        private Channel(string name, Flags flags = Flags.None, int maxUsers = -1, string topic = "")
        {
            ActiveFlags = flags;
            AllowNewUsers = true;
            BannedUsers = new List<GameState>();
            DesignatedHeirs = new Dictionary<GameState, GameState>();
            IsClosed = false;
            MaxUsers = maxUsers;
            Name = name;
            Topic = topic;
            Users = new List<GameState>();
        }

        public bool AcceptUser(GameState user, bool ignoreLimits = false, bool extendedErrors = false)
        {
            if (IsClosed) return false;

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

                    return false;
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

                        return false;
                    }
                }

                if (ActiveFlags.HasFlag(Flags.Restricted) || ActiveFlags.HasFlag(Flags.System))
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"[{Name}] Rejecting user [{user.OnlineName}] for reason [Restricted]");

                    if (extendedErrors)
                        new ChatEvent(ChatEvent.EventIds.EID_CHANNELRESTRICTED, ActiveFlags, 0, "", Name).WriteTo(user.Client);
                    else
                        new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.ChannelIsRestricted).WriteTo(user.Client);

                    return false;
                }
            }

            if (user.ActiveChannel != null && !user.ActiveChannel.RemoveUser(user))
            {
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"[{Name}] Rejecting user [{user.OnlineName}] because of failure to remove from previous channel [{user.ActiveChannel.Name}]");

                if (!extendedErrors)
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.ErrorCaption).WriteTo(user.Client);

                return false;
            }
            user.ActiveChannel = this;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, string.Format("[{0}] Accepting user [{1}] (ignoreLimits: {2})", Name, user.OnlineName, ignoreLimits));

            lock (Users)
            {
                // Add this user to the channel:
                Users.Remove(user);
                Users.Add(user);

                // Tell this user they entered the channel:
                new ChatEvent(ChatEvent.EventIds.EID_CHANNELJOIN, ActiveFlags, 0, RenderOnlineName(user, user), Name).WriteTo(user.Client);

                if (!ActiveFlags.HasFlag(Flags.Silent))
                {
                    foreach (var subuser in Users)
                    {
                        // Tell this user about everyone in the channel:
                        new ChatEvent(ChatEvent.EventIds.EID_USERSHOW, RenderChannelFlags(user, subuser), subuser.Ping, RenderOnlineName(user, subuser), subuser.Statstring).WriteTo(user.Client);

                        // Tell everyone else about this user entering the channel:
                        if (subuser != user)
                        {
                            new ChatEvent(ChatEvent.EventIds.EID_USERJOIN, RenderChannelFlags(subuser, user), user.Ping, RenderOnlineName(subuser, user), user.Statstring).WriteTo(subuser.Client);
                        }
                    }
                }
                else
                {
                    new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, Resources.ChannelIsChatRestricted).WriteTo(user.Client);
                }
            }

            string[] topic = RenderTopic(user).Replace("\r\n", "\n").Replace("\r", "\n").Split("\n");
            if (!(topic.Length == 1 && string.IsNullOrEmpty(topic[0])))
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

                new ChatEvent(ChatEvent.EventIds.EID_BROADCAST, Account.Flags.Admin, -1, Settings.GetString(new string[] { "battlenet", "realm", "name" }, Resources.Battlenet), m).WriteTo(user.Client);
            }

            var autoOp = Settings.GetBoolean(new string[] { "channel", "auto_op" }, false);

            if (((autoOp == true && Count == 1 && IsPrivate()) || Name.ToLower() == "op " + user.OnlineName.ToLower())
                && !(user.ChannelFlags.HasFlag(Account.Flags.Employee) || user.ChannelFlags.HasFlag(Account.Flags.ChannelOp) || user.ChannelFlags.HasFlag(Account.Flags.Admin)))
            {
                UpdateUser(user, user.ChannelFlags | Account.Flags.ChannelOp);
            }

            return true;
        }

        public void BanUser(GameState source, string target, string reason)
        {
            if (!Common.GetClientByOnlineName(target, out var targetClient) || targetClient == null)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.InvalidUser).WriteTo(source.Client);
                return;
            }

            BanUser(source, targetClient, reason);
        }

        public void BanUser(GameState source, GameState target, string reason)
        {
            if (target == null)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.InvalidUser).WriteTo(source.Client);
                return;
            }

            var sourceSudoPrivs = source.ChannelFlags.HasFlag(Account.Flags.Admin)
                || source.ChannelFlags.HasFlag(Account.Flags.Employee);
            var targetSudoPrivs = target.ChannelFlags.HasFlag(Account.Flags.Admin)
                || target.ChannelFlags.HasFlag(Account.Flags.ChannelOp)
                || target.ChannelFlags.HasFlag(Account.Flags.Employee);

            if (targetSudoPrivs && !sourceSudoPrivs)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.YouCannotBanAChannelOperator).WriteTo(source.Client);
                return;
            }

            lock (BannedUsers)
            {
                if (BannedUsers.Contains(target))
                {
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.UserIsAlreadyBanned.Replace("{target}", target.OnlineName)).WriteTo(source.Client);
                    return;
                }
                BannedUsers.Add(target);
            }

            var sourceName = source.OnlineName;
            var maskAdminsInBanMessage = Settings.GetBoolean(new string[] { "battlenet", "emulation", "mask_admins_in_ban_message" }, false);
            if (maskAdminsInBanMessage
                && (source.ChannelFlags.HasFlag(Account.Flags.Employee)
                || source.ChannelFlags.HasFlag(Account.Flags.Admin)))
            {
                sourceName = $"a {Resources.BattlenetRepresentative}";
            }

            var bannedStr = string.IsNullOrEmpty(reason) ? Resources.UserBannedFromChannel : Resources.UserBannedFromChannelWithReason;

            bannedStr = bannedStr.Replace("{reason}", reason);
            bannedStr = bannedStr.Replace("{source}", sourceName);
            bannedStr = bannedStr.Replace("{target}", target.OnlineName);

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, bannedStr));

            lock (Users)
            {
                foreach (var subuser in Users)
                {
                    if (subuser != target) continue;

                    RemoveUser(target);

                    bannedStr = Resources.YouWereBannedFromChannel;

                    bannedStr = bannedStr.Replace("{reason}", reason);
                    bannedStr = bannedStr.Replace("{source}", sourceName);
                    bannedStr = bannedStr.Replace("{target}", target.OnlineName);

                    new ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, bannedStr).WriteTo(target.Client);

                    var theVoid = GetChannelByName(Resources.TheVoid, true);
                    MoveUser(target, theVoid, true);
                }
            }
        }

        public void Close()
        {
            lock (IsClosing)
            {
                if (IsClosed) return;

                if (!Common.ActiveChannels.TryRemove(Name, out _))
                {
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Channel, $"Failed to remove channel [{Name}] from active channel cache");
                }

                if (Users != null)
                {
                    var theVoid = GetChannelByName(Resources.TheVoid, true);
                    if (theVoid != null) lock (Users) foreach (var user in Users) MoveUser(user, theVoid, true);
                }

                BannedUsers = null;
                DesignatedHeirs = null;
                IsClosed = true;
                Users = null;
            }
        }

        public void Designate(GameState designator, GameState heir)
        {
            DesignatedHeirs[designator] = heir;
        }

        public bool DisbandInto(Channel destination)
        {
            if (IsClosed)
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Channel, $"Cannot disband channel [{Name}] because it is closed");
                return false;
            }

            if (destination == this)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Channel, $"Cannot disband channel [{Name}] into itself");
                return false;
            }

            foreach (var user in Users.ToArray()) destination.AcceptUser(user, true, true);
            Close();
            return true;
        }

        public static Channel GetChannelByName(string name, bool autoCreate)
        {
            Channel channel = null;

            if (string.IsNullOrEmpty(name)) return channel;
            if (name[0] == '#') name = name[1..];

            if (Common.ActiveChannels.TryGetValue(name, out channel)
                || !autoCreate || channel != null) return channel;

            var isStatic = GetStaticChannel(name, out var staticName, out var staticFlags, out var staticMaxUsers, out var staticTopic, out var staticProducts);

            if (!isStatic)
            {
                channel = new Channel(name, Flags.None);
            }
            else
            {
                channel = new Channel(staticName, staticFlags, staticMaxUsers, staticTopic);
            }

            if (!Common.ActiveChannels.TryAdd(channel.Name, channel))
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Channel, $"Failed to add channel [{channel.Name}] to active channel cache; using already existing channel");
                if (!Common.ActiveChannels.TryGetValue(channel.Name, out channel))
                {
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Channel, $"Failed to find existing channel [{channel.Name}]");
                }
            }

            return channel;
        }

        protected static string GetNextUserId()
        {
            return DateTime.Now.ToString("HH:mm:ss.ffffff");
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
                        var product = Product.FromBytes(Encoding.ASCII.GetBytes(productJson.ToString()), true);
                        _list.Add(product);
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

        public string GetUsersAsString(GameState context)
        {
            if (ActiveFlags.HasFlag(Flags.Silent)) return string.Empty;

            var names = new LinkedList<string>();
            lock (Users)
            {
                foreach (var user in Users)
                {
                    var userName = RenderOnlineName(context, user);
                    if (user.ChannelFlags.HasFlag(Account.Flags.Employee) ||
                        user.ChannelFlags.HasFlag(Account.Flags.ChannelOp) ||
                        user.ChannelFlags.HasFlag(Account.Flags.Admin))
                    {
                        names.AddFirst($"[{userName.ToUpper()}]");
                    } else
                    {
                        names.AddLast(userName);
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
                    s += $"{n}{Battlenet.Common.NewLine}";
                }
                i++;
            }
            if (i % 2 != 0) s = s[0..^2]; // trim trailing comma

            return s;
        }

        public bool IsModerated()
        {
            return ActiveFlags.HasFlag(Flags.Moderated);
        }

        public bool IsPrivate()
        {
            return !ActiveFlags.HasFlag(Flags.Public);
        }

        public bool IsProductSpecific()
        {
            return ActiveFlags.HasFlag(Flags.ProductSpecific);
        }

        public bool IsPublic()
        {
            return ActiveFlags.HasFlag(Flags.Public);
        }

        public bool IsRestricted()
        {
            return ActiveFlags.HasFlag(Flags.Restricted);
        }

        public bool IsSilent()
        {
            return ActiveFlags.HasFlag(Flags.Silent);
        }

        public bool IsTechSupport()
        {
            return ActiveFlags.HasFlag(Flags.TechSupport);
        }

        public void KickUser(GameState source, string target, string reason)
        {
            GameState targetClient = null;

            lock (Users)
            {
                foreach (var user in Users)
                {
                    if (StringComparer.OrdinalIgnoreCase.Compare(user.OnlineName, target) == 0)
                    {
                        targetClient = user;
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

            var sourceName = source.OnlineName;
            var maskAdminsInKickMessage = Settings.GetBoolean(new string[] { "battlenet", "emulation", "mask_admins_in_kick_message" }, false);
            if (maskAdminsInKickMessage
                && (source.ChannelFlags.HasFlag(Account.Flags.Employee)
                || source.ChannelFlags.HasFlag(Account.Flags.Admin)))
            {
                sourceName = $"a {Resources.BattlenetRepresentative}";
            }

            var kickedStr = reason.Length > 0 ? Resources.UserKickedFromChannelWithReason : Resources.UserKickedFromChannel;

            kickedStr = kickedStr.Replace("{reason}", reason);
            kickedStr = kickedStr.Replace("{source}", sourceName);
            kickedStr = kickedStr.Replace("{target}", target);

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, kickedStr));

            RemoveUser(targetClient);

            kickedStr = Resources.YouWereKickedFromChannel;

            kickedStr = kickedStr.Replace("{reason}", reason);
            kickedStr = kickedStr.Replace("{source}", sourceName);
            kickedStr = kickedStr.Replace("{target}", target);

            new ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, kickedStr).WriteTo(targetClient.Client);

            var theVoid = GetChannelByName(Resources.TheVoid, true);
            MoveUser(targetClient, theVoid, true);
        }

        public static bool MoveUser(GameState client, string name, bool autoCreate = true, bool ignoreLimits = true, bool extendedErrors = false)
        {
            return MoveUser(client, GetChannelByName(name, autoCreate), ignoreLimits, extendedErrors);
        }

        public static bool MoveUser(GameState client, Channel channel, bool ignoreLimits = true, bool extendedErrors = false)
        {
            if (client == null) return false;
            if (channel == null) return false;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"Moving user [{client.OnlineName}] {(client.ActiveChannel != null ? $"from [{client.ActiveChannel.Name}] " : "")}to [{channel.Name}] (ignoreLimits: {ignoreLimits})");

            if (channel == client.ActiveChannel && channel.Count == 1)
            {
                return channel.Resync(); // Avoid calling Close() when private channel empties.
            }
            else
            {
                return channel.AcceptUser(client, ignoreLimits, extendedErrors);
            }
        }

        public bool RemoveUser(GameState user)
        {
            bool removed = false;
            lock (Users) removed = Users.Remove(user);

            // If the user is not in the Users list, then we give up here
            if (!removed)
            {
                if (Count == 0 && !ActiveFlags.HasFlag(Flags.Public)) Close();
                return removed;
            }

            lock (user)
            {
                var remoteAddress = IPAddress.Parse(user.Client.RemoteEndPoint.ToString().Split(':')[0]);
                var squelched = user.SquelchedIPs.Contains(remoteAddress);
                var flags = squelched ? user.ChannelFlags | Account.Flags.Squelched : user.ChannelFlags & ~Account.Flags.Squelched;

                user.ActiveChannel = null;
                var wasChannelOp = user.ChannelFlags.HasFlag(Account.Flags.Employee)
                    || user.ChannelFlags.HasFlag(Account.Flags.ChannelOp)
                    || user.ChannelFlags.HasFlag(Account.Flags.Admin);
                user.ChannelFlags &= ~Account.Flags.ChannelOp; // remove channel op

                if (!ActiveFlags.HasFlag(Flags.Silent))
                {
                    var emptyStatstring = new byte[0];
                    lock (Users)
                    {
                        foreach (var subuser in Users)
                        {
                            // Tell everyone else about this user leaving the channel:
                            new ChatEvent(ChatEvent.EventIds.EID_USERLEAVE, RenderChannelFlags(subuser, user), user.Ping, RenderOnlineName(subuser, user), emptyStatstring).WriteTo(subuser.Client);
                        }
                    }
                }

                lock (DesignatedHeirs)
                {
                    var heirExists = DesignatedHeirs.TryGetValue(user, out var heir) && heir != null;

                    if (wasChannelOp && heirExists)
                    {
                        if (heir != null && heir.ActiveChannel == this && !heir.ChannelFlags.HasFlag(Account.Flags.ChannelOp))
                        {
                            // Promote the designated heir.
                            UpdateUser(heir, heir.ChannelFlags | Account.Flags.ChannelOp);
                        }
                    }

                    if (heirExists && !DesignatedHeirs.Remove(user, out _))
                    {
                        Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Channel, $"Failed to remove designated heir [{heir.OnlineName}] from channel [{this.Name}]");
                    }
                }

                // If autoOp is enabled and channel is not public, does not start with "clan " or "op ", then promote next user by oldest entry to channel operator.
                var autoOp = Settings.GetBoolean(new string[] { "channel", "auto_op" }, false);
                if (autoOp && !(IsPublic() || Name.StartsWith("clan ", true, CultureInfo.InvariantCulture) || Name.StartsWith("op ", true, CultureInfo.InvariantCulture)))
                {
                    bool hasOperator = false;
                    lock (Users)
                    {
                        foreach (var subuser in Users)
                        {
                            if (subuser.HasAdmin(includeChannelOp: true))
                            {
                                hasOperator = true;
                                break;
                            }
                        }
                        if (!hasOperator && Count > 0) UpdateUser(Users[0], Users[0].ChannelFlags | Account.Flags.ChannelOp);
                    }
                }
            }

            if (Count == 0 && !ActiveFlags.HasFlag(Flags.Public)) Close();
            return removed;
        }

        /**
         * <remarks>Renders the in-channel flags in context-aware situations, used when considering the squelched flag is necessary.</remarks>
         * <param name="context">The user that will receive this chat event, whom this render will be presented to.</param>
         * <param name="target">The target user that is being rendered for the context user.</param>
         */
        public static Account.Flags RenderChannelFlags(GameState context, GameState target)
        {
            var targetFlags = target.ChannelFlags;
            var targetRemoteAddress = IPAddress.Parse(target.Client.RemoteEndPoint.ToString().Split(':')[0]);
            var targetSquelched = context.SquelchedIPs.Contains(targetRemoteAddress);

            // Add or remove squelched flag:
            targetFlags = targetSquelched ? targetFlags | Account.Flags.Squelched : targetFlags & ~Account.Flags.Squelched;

            return targetFlags;
        }

        /**
         * <remarks>Renders the full online name of a user in context-aware situations, used with Diablo II character names and realms, and gateway names.</remarks>
         * <param name="context">The user that will receive this chat event, whom this render will be presented to.</param>
         * <param name="target">The target user that is being rendered for the context user.</param>
         */
        public static string RenderOnlineName(GameState context, GameState target)
        {
            var targetName = target.OnlineName;

            // Add Diablo II character name:
            if (Product.IsDiabloII(context.Product))
            {
                targetName = $"{Encoding.UTF8.GetString(target.CharacterName)}*{targetName}";
            }

            // Add Warcraft III internal identifier:
            if (context == target && Product.IsWarcraftIII(context.Product))
            {
                targetName = $"w#{targetName}";
            }

            // TODO: Suffix "#{gateway}" or "@{gateway}" name, such as: JoeUser#Azeroth

            return targetName;
        }

        public string RenderTopic(GameState receiver)
        {
            var r = Topic;

            r = r.Replace("{account}", (string)receiver.ActiveAccount.Get(Account.UsernameKey));
            r = r.Replace("{channel}", Name);
            r = r.Replace("{channelMaxUsers}", MaxUsers.ToString());
            r = r.Replace("{channelUserCount}", Count.ToString());
            r = r.Replace("{game}", Product.ProductName(receiver.Product, false));
            r = r.Replace("{gameFull}", Product.ProductName(receiver.Product, true));
            r = r.Replace("{ping}", receiver.Ping.ToString() + "ms");
            r = r.Replace("{user}", receiver.OnlineName);
            r = r.Replace("{username}", receiver.OnlineName);
            r = r.Replace("{userName}", receiver.OnlineName);
            r = r.Replace("{userPing}", receiver.Ping.ToString() + "ms");

            return r;
        }

        public bool Resync()
        {
            var args = new Dictionary<string, object> {{ "chatEvent", null }};
            var msg = new SID_CHATEVENT();

            lock (Users)
            {
                foreach (var user in Users)
                {
                    // Tell users they re-entered the channel:
                    args["chatEvent"] = new ChatEvent(ChatEvent.EventIds.EID_CHANNELJOIN, ActiveFlags, 0, RenderOnlineName(user, user), Name);
                    msg.Invoke(new MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args));
                    user.Client.Send(msg.ToByteArray(user.Client.ProtocolType));

                    // Show users in channel or display info about no chat:
                    if (!ActiveFlags.HasFlag(Flags.Silent))
                    {
                        foreach (var subuser in Users)
                        {
                            args["chatEvent"] = new ChatEvent(ChatEvent.EventIds.EID_USERSHOW, RenderChannelFlags(user, subuser), subuser.Ping, RenderOnlineName(user, subuser), subuser.Statstring);
                            msg.Invoke(new MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args));
                            user.Client.Send(msg.ToByteArray(user.Client.ProtocolType));
                        }
                    }
                    else
                    {
                        args["chatEvent"] = new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, Resources.ChannelIsChatRestricted);
                        msg.Invoke(new MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args));
                        user.Client.Send(msg.ToByteArray(user.Client.ProtocolType));
                    }

                    // Channel topic:
                    string[] topic = RenderTopic(user).Replace("\r\n", "\n").Replace("\r", "\n").Split("\n");
                    foreach (var line in topic)
                    {
                        args["chatEvent"] = new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line);
                        msg.Invoke(new MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args));
                        user.Client.Send(msg.ToByteArray(user.Client.ProtocolType));
                    }
                }
            }

            return true;
        }

        public void SetActiveFlags(Flags newFlags)
        {
            ActiveFlags = newFlags;
            Resync();
        }

        public void SetAllowNewUsers(bool allowNewUsers)
        {
            AllowNewUsers = allowNewUsers;

            var r = allowNewUsers ? Resources.ChannelIsNowPublic : Resources.ChannelIsNowPrivate;
            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, r));
        }

        public void SetMaxUsers(int maxUsers)
        {
            MaxUsers = maxUsers;

            var r = Resources.ChannelMaxUsersChanged.Replace("{maxUsers}", $"{maxUsers}");
            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, r));
        }

        public void SetName(string newName)
        {
            var oldName = Name;
            Name = newName;
            if (Count == 0) return;

            var r = Resources.ChannelWasRenamed.Replace("{oldName}", oldName).Replace("{newName}", newName);
            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, r));
            Resync();
        }

        public void SetTopic(string newTopic)
        {
            Topic = newTopic;

            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, Resources.ChannelTopicChanged));

            lock (Users)
            {
                foreach (var user in Users)
                {
                    string[] topic = RenderTopic(user).Replace("\r\n", "\n").Replace("\r", "\n").Split("\n");
                    foreach (var line in topic) new ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line).WriteTo(user.Client);
                }
            }
        }

        public void SquelchUpdate(GameState client)
        {
            if (client == null) throw new NullReferenceException("Client parameter must not be null");

            lock (Users)
            {
                foreach (var user in Users)
                {
                    new ChatEvent(ChatEvent.EventIds.EID_USERUPDATE, RenderChannelFlags(client, user), user.Ping, RenderOnlineName(client, user), user.Statstring).WriteTo(client.Client);
                }
            }
        }

        public void UnBanUser(GameState source, string target)
        {
            if (!Common.GetClientByOnlineName(target, out var targetClient) || targetClient == null)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.UserNotLoggedOn).WriteTo(source.Client);
                return;
            }

            UnBanUser(source, targetClient);
        }

        public void UnBanUser(GameState source, GameState target)
        {
            if (target == null)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.InvalidUser).WriteTo(source.Client);
                return;
            }

            var wasBanned = false;
            lock (BannedUsers)
            {
                if (BannedUsers.Contains(target))
                {
                    BannedUsers.Remove(target);
                    wasBanned = true;
                }
            }

            if (!wasBanned)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.UserIsNotBanned.Replace("{target}", target.OnlineName)).WriteTo(source.Client);
                return;
            }

            var sourceName = source.OnlineName;
            var maskAdminsInBanMessage = Settings.GetBoolean(new string[] { "battlenet", "emulation", "mask_admins_in_ban_message" }, false);
            if (maskAdminsInBanMessage
                && (source.ChannelFlags.HasFlag(Account.Flags.Employee)
                || source.ChannelFlags.HasFlag(Account.Flags.Admin)))
            {
                sourceName = $"a {Resources.BattlenetRepresentative}";
            }

            var bannedStr = Resources.UserUnBannedFromChannel;
            bannedStr = bannedStr.Replace("{source}", sourceName);
            bannedStr = bannedStr.Replace("{target}", target.OnlineName);
            WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, bannedStr));
        }

        /**
         * <remarks>This function should only be called if any of the attributes were modified outside of this class.</remarks>
         */
        public bool UpdateUser(GameState client)
        {
            return UpdateUser(client, client.ChannelFlags, client.Ping, client.Statstring);
        }

        public bool UpdateUser(GameState client, Account.Flags flags)
        {
            return UpdateUser(client, flags, client.Ping, client.Statstring);
        }

        public bool UpdateUser(GameState client, Int32 ping)
        {
            return UpdateUser(client, client.ChannelFlags, ping, client.Statstring);
        }

        public bool UpdateUser(GameState client, byte[] statstring)
        {
            return UpdateUser(client, client.ChannelFlags, client.Ping, statstring);
        }

        public bool UpdateUser(GameState client, string statstring)
        {
            return UpdateUser(client, client.ChannelFlags, client.Ping, statstring);
        }

        /**
         * <remarks>Updates the client's flags, ping, and/or statstring, and then sends a ChatEvent to tell other clients about the change.</remarks>
         * <param name="client">The client which is being updated or was updated earlier outside of this context.</param>
         * <param name="flags">The new flags. (Optional)</param>
         * <param name="ping">The new ping. (Optional)</param>
         * <param name="statstring">The new statstring. (Optional)</param>
         * <param name="forceEvent">Whether to send a ChatEvent regardless if no alteration was made. Useful if the client was updated before calling this function, instead of using the properties of this function to alter the client.</param>
         */
        public bool UpdateUser(GameState client, Account.Flags flags, Int32 ping, byte[] statstring, bool forceEvent = false)
        {
            var changed = false;

            if (client.ChannelFlags != flags)
            {
                client.ChannelFlags = flags;
                changed = true;
            }

            if (client.Ping != ping)
            {
                client.Ping = ping;
                changed = true;
            }

            if (client.Statstring != statstring)
            {
                client.Statstring = statstring;
                changed = true;
            }

            if (!changed && !forceEvent) return false; // don't emit ChatEvent for unnecessary calls to UpdateUser() if nothing changed

            if (Users == null) return true;
            lock (Users)
            {
                foreach (var user in Users)
                {
                    new ChatEvent(ChatEvent.EventIds.EID_USERUPDATE, RenderChannelFlags(user, client), client.Ping, RenderOnlineName(user, client), client.Statstring).WriteTo(user.Client);
                }
            }

            return true;
        }

        public bool UpdateUser(GameState client, Account.Flags flags, Int32 ping, string statstring)
        {
            return UpdateUser(client, flags, ping, Encoding.UTF8.GetBytes(statstring));
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

                    if (ChatEvent.EventIdIsChatMessage(chatEvent.EventId) && RenderChannelFlags(owner, user).HasFlag(Account.Flags.Squelched))
                    {
                        // "user" has decided to squelch messages from "owner"
                        continue;
                    }

                    msg.Invoke(new MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args));
                    user.Client.Send(msg.ToByteArray(user.Client.ProtocolType));
                }
            }
        }

        /**
         * <remarks>Writes a chat message on behalf of <paramref name="owner"/>, either EID_TALK or EID_EMOTE depending on <paramref name="emote"/>, to the channel.</remarks>
         * <param name="owner">The user that is sending this message.</param>
         * <param name="message">The message that the <paramref name="owner"/> wishes to send to the channel.</param>
         * <param name="emote">Whether to write the message with EID_TALK (false) or EID_EMOTE (true) event id type. Defaults to EID_TALK (false).</param>
         */
        public void WriteChatMessage(GameState owner, byte[] message, bool emote = false)
        {
            var msg = new SID_CHATEVENT();

            lock (Users)
            {
                foreach (var user in Users)
                {
                    if (owner != null && user == owner && !emote)
                    {
                        // Dropping EID_TALK from being echoed back to sender
                        continue;
                    }

                    var flags = RenderChannelFlags(user, owner);
                    if (flags.HasFlag(Account.Flags.Squelched))
                    {
                        // "user" has decided to squelch messages from "owner"
                        continue;
                    }

                    var chatEvent = new ChatEvent(emote ? ChatEvent.EventIds.EID_EMOTE : ChatEvent.EventIds.EID_TALK, flags, owner.Ping, RenderOnlineName(user, owner), message);

                    msg.Invoke(new MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, new Dictionary<string, dynamic>() {{ "chatEvent", chatEvent }}));
                    user.Client.Send(msg.ToByteArray(user.Client.ProtocolType));
                }
            }
        }
    }
}
