using Atlasd.Battlenet;
using Atlasd.Battlenet.Protocols.Game;
using System;

namespace Atlasd.Battlenet.Channels
{
    interface IChannel
    {
        public const FlagIds TheVoidFlags = FlagIds.Public | FlagIds.Silent;

        [Flags]
        public enum FlagIds : UInt32
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

        public bool Accept(GameState target, bool ignoreLimits = false, bool extendedErrors = false);
        public bool AddBan(GameState source, GameState target) => Kick(source, target, Array.Empty<byte>());
        public bool AddBan(GameState source, GameState target, byte[] reason);
        public void Close();
        public bool Designate(GameState designator, GameState heir);
        public bool Disband();
        public bool DisbandInto(IChannel destination);
        public bool GetAcceptNewMembers();
        public FlagIds GetFlags();
        public string GetMembersAsCommaSeparatedLines(GameState source);
        public int GetMemberMaxCount();
        public byte[] GetName();
        public byte[] GetTopic(bool format) => GetTopic(format, null);
        public byte[] GetTopic(bool format, GameState target);
        public bool IsBannedIP(System.Net.IPAddress value);
        public bool IsBannedName(byte[] value);
        public bool IsChat();
        public bool IsGlobal();
        public bool IsModerated();
        public bool IsPrivate();
        public bool IsProductSpecific();
        public bool IsPublic();
        public bool IsRedirected();
        public bool IsRestricted();
        public bool IsSilent();
        public bool IsSystem();
        public bool IsTechSupport();
        public bool Kick(GameState source, GameState target) => Kick(source, target, Array.Empty<byte>());
        public bool Kick(GameState source, GameState target, byte[] reason);
        public bool QueueChatEvent(ChatEvent chatEvent) => QueueChatEvent(null, chatEvent);
        public bool QueueChatEvent(GameState owner, ChatEvent chatEvent);
        public System.Threading.Tasks.Task ProcessChatEvents();
        public bool Remove(GameState target);
        public bool RemoveBan(GameState source, GameState target);
        public bool SetAcceptNewMembers(bool value);
        public bool SetFlags(FlagIds newFlags);
        public bool SetMemberMaxCount(int maxCount);
        public bool SetName(byte[] newName);
        public bool SetTopic(byte[] newTopic);
        public bool Sync();
        public bool UpdateMember(GameState target) => UpdateMember(target, target.ChannelFlags, target.Ping, target.Statstring);
        public bool UpdateMember(GameState target, Account.Flags flags) => UpdateMember(target, flags, target.Ping, target.Statstring);
        public bool UpdateMember(GameState target, Int32 ping) => UpdateMember(target, target.ChannelFlags, ping, target.Statstring);
        public bool UpdateMember(GameState target, byte[] statstring) => UpdateMember(target, target.ChannelFlags, target.Ping, statstring);
        public bool UpdateMember(GameState target, Account.Flags flags, Int32 ping, byte[] statstring);
    }
}
