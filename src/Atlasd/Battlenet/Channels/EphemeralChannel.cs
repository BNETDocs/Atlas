using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Atlasd.Battlenet.Channels
{
    class EphemeralChannel : IChannel
    {
        public static readonly string TheVoid = Atlasd.Localization.Resources.TheVoid;
        public static readonly byte[] TheVoidBytes = Encoding.UTF8.GetBytes(TheVoid);

        protected bool AcceptNewMembers;
        protected IList<IPAddress> BannedIPs;
        protected IList<byte[]> BannedNames;
        protected ConcurrentBag<KeyValuePair<GameState, ChatEvent>> ChatEventQueue;
        private bool Closed;
        private readonly object IsClosing = new object();
        protected IDictionary<GameState, GameState> DesignatedHeirs;
        protected IChannel.FlagIds Flags;
        protected int MemberMaxCount;
        protected IList<GameState> Members;
        protected byte[] Name;
        protected byte[] Topic;

        public EphemeralChannel(byte[] name, IChannel.FlagIds flags = IChannel.FlagIds.None)
        {
            SetAcceptNewMembers(true);
            BannedIPs = new List<IPAddress>();
            BannedNames = new List<byte[]>();
            ChatEventQueue = new ConcurrentBag<KeyValuePair<GameState, ChatEvent>>();
            Closed = false;
            DesignatedHeirs = new Dictionary<GameState, GameState>();
            SetFlags(flags);
            SetMemberMaxCount(Channel.DefaultMaxMemberCount());
            Members = new List<GameState>();
            SetName(name);
            SetTopic(Array.Empty<byte>());
        }

        public bool Accept(GameState target, bool ignoreLimits = false, bool extendedErrors = false)
        {
            if (Closed) return false;

            var nameStr = Encoding.UTF8.GetString(Name);
            var targetUsername = Encoding.UTF8.GetBytes(target.Username);

            if (!target.HasAdmin(includeChannelOp: false))
            {
                if (GetMemberCount() >= GetMemberMaxCount())
                {
                    if (extendedErrors)
                        QueueChatEvent(target, new ChatEvent(ChatEvent.EventIds.EID_CHANNELFULL, (uint)GetFlags(), 0, string.Empty, GetName()));
                    else
                        QueueChatEventError(target, Resources.ChannelIsFull);
                    return false;
                }

                if (IsRestricted())
                {
                    if (extendedErrors)
                        QueueChatEvent(target, new ChatEvent(ChatEvent.EventIds.EID_CHANNELRESTRICTED, (uint)GetFlags(), 0, string.Empty, GetName()));
                    else
                        QueueChatEventError(target, Resources.ChannelIsRestricted);
                    return false;
                }

                if (IsBannedIP(target.Client.RemoteIPAddress) || IsBannedName(targetUsername))
                {
                    if (extendedErrors)
                        QueueChatEvent(target, new ChatEvent(ChatEvent.EventIds.EID_CHANNELRESTRICTED, (uint)GetFlags(), 0, string.Empty, GetName()));
                    else
                        QueueChatEventError(target, Resources.YouAreBannedFromThatChannel);
                    return false;
                }

                if (nameStr.StartsWith("clan ", true, CultureInfo.InvariantCulture)) return false;
            }

            return false; // TODO
        }

        public bool AddBan(GameState source, GameState target, byte[] reason)
        {
            if (Closed) return false;

            if (!source.HasAdmin(includeChannelOp: true))
            {
                QueueChatEventError(source, Resources.YouAreNotAChannelOperator);
                return false;
            }

            var sourceUsername = Encoding.UTF8.GetBytes(source.Username);
            var targetUsername = Encoding.UTF8.GetBytes(target.Username);

            var sourceIPAddress = source.Client.RemoteIPAddress;
            var targetIPAddress = target.Client.RemoteIPAddress;

            return false; // TODO

            /*BannedIPs.Add(targetIPAddress);
            BannedNames.Add(targetUsername);    

            return true;*/
        }

        /**
         * <remarks>Closes the channel and removes it from the server. Any remaining members are disbanded into The Void.</remarks>
         */
        public void Close()
        {
            lock (IsClosing)
            {
                if (Closed) return;
                try
                {
                    AcceptNewMembers = false;
                    MemberMaxCount = 0;
                    Channel.RemoveChannel(this);
                    Disband();

                    lock (BannedIPs) BannedIPs.Clear();
                    lock (BannedNames) BannedNames.Clear();
                    ChatEventQueue.Clear();
                    lock (DesignatedHeirs) DesignatedHeirs.Clear();
                }
                finally
                {
                    Closed = true;
                }
            }
        }

        /**
         * <remarks>Sets a channel member's designated heir.</remarks>
         * <param name="designator">The user which when leaving will give up their channel operator.</param>
         * <param name="heir">The user which will become channel operator when the designator gives up theirs.</param>
         */
        public bool Designate(GameState designator, GameState heir)
        {
            if (Closed) return false;
            lock (DesignatedHeirs) DesignatedHeirs[designator] = heir;
            return true;
        }

        /**
         * <remarks>Disbands the channel members to The Void.</remarks>
         */
        public bool Disband()
        {
            if (!Channel.FindChannel(TheVoidBytes, true, out IChannel theVoid)) return false;
            return DisbandInto(theVoid);
        }

        /**
         * <remarks>Disbands the channel members to a specific channel.</remarks>
         * <param name="destination">The destination channel to disband this channel's members into.</param>
         */
        public bool DisbandInto(IChannel destination)
        {
            lock (Members)
            {
                if (Members.Count > 0)
                {
                    foreach (var member in Members) destination.Accept(member, true, true);
                    Members.Clear();
                }
            }

            return true;
        }

        public bool GetAcceptNewMembers() => AcceptNewMembers;

        public IChannel.FlagIds GetFlags() => Flags;

        public string GetMembersAsCommaSeparatedLines(GameState source)
        {
            // [OPERATOR], MemBeR,
            // MemBeR#2

            uint count = 0;
            string buffer = string.Empty;
            lock (Members)
            {
                foreach (var member in Members)
                {
                    string memberUsername = member.Username;
                    if (member.HasAdmin(includeChannelOp: true)) memberUsername = $"[{memberUsername.ToUpperInvariant()}]";
                    buffer += count++ % 2 == 0 ? memberUsername : $", {memberUsername},{Battlenet.Common.NewLine}";
                }
            }
            if (buffer.EndsWith($",{Battlenet.Common.NewLine}")) buffer = buffer[0..(-1 - Battlenet.Common.NewLine.Length)];
            return buffer;
        }

        public int GetMemberCount() => Members.Count;

        public int GetMemberMaxCount() => MemberMaxCount;

        public byte[] GetName() => Name;

        public byte[] GetTopic(bool format, GameState target)
        {
            if (!format) return Topic; // skip byte[]->string->byte[] conversion below if not formatting
            var t = Encoding.UTF8.GetString(Topic);

            t = t.Replace("{channel}", Encoding.UTF8.GetString(GetName()), true, CultureInfo.InvariantCulture);
            t = t.Replace("{channelMaxUsers}", GetMemberMaxCount().ToString(CultureInfo.InvariantCulture), true, CultureInfo.InvariantCulture);
            t = t.Replace("{channelUserCount}", Members.Count.ToString(CultureInfo.InvariantCulture), true, CultureInfo.InvariantCulture);

            if (target != null)
            {
                t = t.Replace("{account}", (string)target.ActiveAccount.Get(Account.UsernameKey), true, CultureInfo.InvariantCulture);
                t = t.Replace("{game}", Product.ProductName(target.Product, false), true, CultureInfo.InvariantCulture);
                t = t.Replace("{gameFull}", Product.ProductName(target.Product, true), true, CultureInfo.InvariantCulture);
                t = t.Replace("{ping}", target.Ping.ToString(CultureInfo.InvariantCulture) + "ms", true, CultureInfo.InvariantCulture);
                t = t.Replace("{user}", target.OnlineName, true, CultureInfo.InvariantCulture);
                t = t.Replace("{userName}", target.OnlineName, true, CultureInfo.InvariantCulture);
                t = t.Replace("{userPing}", target.Ping.ToString(CultureInfo.InvariantCulture) + "ms", true, CultureInfo.InvariantCulture);
            }

            return Encoding.UTF8.GetBytes(t);
        }

        public bool IsBannedIP(IPAddress value)
        {
            return false; // TODO
        }

        public bool IsBannedName(byte[] value)
        {
            return false; // TODO
        }

        public bool IsChat() => Flags.HasFlag(IChannel.FlagIds.Chat);
        public bool IsGlobal() => Flags.HasFlag(IChannel.FlagIds.Global);
        public bool IsModerated() => Flags.HasFlag(IChannel.FlagIds.Moderated);
        public bool IsPrivate() => !Flags.HasFlag(IChannel.FlagIds.Public);
        public bool IsProductSpecific() => Flags.HasFlag(IChannel.FlagIds.ProductSpecific);
        public bool IsPublic() => Flags.HasFlag(IChannel.FlagIds.Public);
        public bool IsRedirected() => Flags.HasFlag(IChannel.FlagIds.Redirected);
        public bool IsRestricted() => Flags.HasFlag(IChannel.FlagIds.Restricted);
        public bool IsSilent() => Flags.HasFlag(IChannel.FlagIds.Silent);
        public bool IsSystem() => Flags.HasFlag(IChannel.FlagIds.System);
        public bool IsTechSupport() => Flags.HasFlag(IChannel.FlagIds.TechSupport);

        public bool Kick(GameState source, GameState target, byte[] reason)
        {
            if (Closed) return false;

            if (!source.HasAdmin(includeChannelOp: true))
            {
                QueueChatEventError(source, Resources.YouAreNotAChannelOperator);
                return false;
            }

            return false; // TODO
        }

        public async Task ProcessChatEvents()
        {
            try
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Channel, "ProcessChatEvents() started");

                while (!Closed)
                {
                    while (ChatEventQueue.TryTake(out var pair))
                    {
                        GameState gameState = pair.Key;
                        ChatEvent chatEvent = pair.Value;

                        if (gameState.Client == null || !gameState.Client.Connected) continue;

                        gameState.Client.Send(chatEvent.ToByteArray(gameState.Client.ProtocolType.Type));
                    }

                    await Task.Delay(10);
                }
            }
            finally
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Channel, "ProcessChatEvents() finished");
            }
        }

        public bool QueueChatEvent(GameState owner, ChatEvent chatEvent)
        {
            if (Closed || ChatEventQueue == null) return false;
            ChatEventQueue.Add(new KeyValuePair<GameState, ChatEvent>(owner, chatEvent));
            return true;
        }

        protected bool QueueChatEventError(GameState owner, byte[] message) => QueueChatEvent(owner, new ChatEvent(ChatEvent.EventIds.EID_ERROR, owner.ChannelFlags, owner.Ping, owner.Username, message));

        protected bool QueueChatEventError(GameState owner, string message) => QueueChatEventError(owner, Encoding.UTF8.GetBytes(message));

        protected bool QueueChatEventInfo(GameState owner, byte[] message) => QueueChatEvent(owner, new ChatEvent(ChatEvent.EventIds.EID_INFO, owner.ChannelFlags, owner.Ping, owner.Username, message));

        protected bool QueueChatEventInfo(GameState owner, string message) => QueueChatEventInfo(owner, Encoding.UTF8.GetBytes(message));

        public bool Remove(GameState target)
        {
            if (Closed) return false;
            if (Members.Count == 0 && IsPrivate()) Close();
            return false; // TODO
        }

        public bool RemoveBan(GameState source, GameState target)
        {
            if (Closed) return false;

            if (!source.HasAdmin(includeChannelOp: true))
            {
                QueueChatEventError(source, Resources.YouAreNotAChannelOperator);
                return false;
            }

            return false; // TODO
        }

        public bool SetAcceptNewMembers(bool value)
        {
            if (Closed) return false;
            AcceptNewMembers = value;
            return true;
        }

        public bool SetFlags(IChannel.FlagIds newFlags)
        {
            if (Closed) return false;
            Flags = newFlags;
            return Members.Count == 0 ? true : Sync();
        }

        public bool SetMemberMaxCount(int maxCount)
        {
            if (Closed) return false;
            if (maxCount < -1) throw new ArgumentException("Integer must be -1 (no max limit), 0 or higher.");
            MemberMaxCount = maxCount;
            return true;
        }

        public bool SetName(byte[] newName)
        {
            if (Closed) return false;
            if (newName.Length == 0) throw new ArgumentException("Name must be non-empty");
            Name = newName;
            return Members.Count == 0 ? true : Sync();
        }

        public bool SetTopic(byte[] newTopic)
        {
            if (Closed) return false;
            Topic = newTopic;
            return true;
        }

        public bool Sync()
        {
            if (Closed) return false;
            return false; // TODO
        }

        public bool UpdateMember(GameState target, Account.Flags flags, Int32 ping, byte[] statstring)
        {
            if (Closed) return false;
            return false; // TODO
        }
    }
}
