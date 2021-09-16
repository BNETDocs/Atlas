using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet
{
    class Clan : IDisposable
    {
        public enum Ranks : byte
        {
            Probation = 0, // Peon
            Initiate  = 1, // Peon
            Member    = 2, // Grunt
            Officer   = 3, // Shaman
            Leader    = 4, // Chieftain
            NotInClan = 255,
        }

        /**
         * <remarks>Used with the SID_CLAN-prefixed <see cref="Message"/> classes.</remarks>
         */
        public enum Results : byte
        {
            Success = 0,
            NameInUse = 1,
            TooSoon = 2,
            NotEnough = 3,
            Decline = 4,
            Unavailable = 5,
            Accept = 6,
            NotAuthorized = 7,
            NotAllowed = 8,
            ClanIsFull = 9,
            BadTag = 10,
            BadName = 11,
            UserNotFound = 12,
        }

        public Channel ActiveChannel { get; protected set; }
        public byte[] Tag { get; protected set; }
        public byte[] Name { get; protected set; }
        public ConcurrentDictionary<byte[], Ranks> Users { get; protected set; }

        public Clan(byte[] tag, byte[] name, IDictionary<byte[], Ranks> users = null)
        {
            SetName(name);
            SetTag(tag);

            ActiveChannel = Channel.GetChannelByName($"Clan {Encoding.UTF8.GetString(tag).Replace("\0", "")}", true);
            Users = new ConcurrentDictionary<byte[], Ranks>(users);
        }

        public void Close()
        {
            if (ActiveChannel != null)
            {
                if (ActiveChannel.Count == 0) ActiveChannel.Dispose();
                ActiveChannel = null;
            }
        }

        public void Dispose()
        {
            Close();
        }

        public bool AddUser(byte[] username, Ranks rank)
        {
            return Users.TryAdd(username, rank);
        }

        public bool ContainsUser(byte[] username)
        {
            var usernameStr = Encoding.UTF8.GetString(username);
            var users = Users.ToArray();
            foreach (var (n, _) in users) // discard rank
            {
                string nStr = Encoding.UTF8.GetString(n);
                if (string.Equals(usernameStr, nStr, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool GetUserRank(byte[] username, out Ranks rank)
        {
            rank = Ranks.NotInClan;
            var usernameStr = Encoding.UTF8.GetString(username);
            var users = Users.ToArray();
            foreach (var (n, r) in users)
            {
                string nStr = Encoding.UTF8.GetString(n);
                if (string.Equals(usernameStr, nStr, StringComparison.CurrentCultureIgnoreCase))
                {
                    rank = r;
                    return true;
                }
            }
            return false;
        }

        public bool RemoveUser(byte[] username)
        {
            return Users.TryRemove(username, out _);
        }

        public void SetName(byte[] name)
        {
            if (name.Length < 1)
                throw new ArgumentOutOfRangeException($"Clan name must be at least 1 byte in length");

            Name = name;
        }

        public void SetTag(byte[] tag)
        {
            if (tag.Length != 4)
                throw new ArgumentOutOfRangeException($"Clan tag must be exactly 4 bytes in length");

            Tag = tag;

            WriteClanInfo();

            if (ActiveChannel != null)
                ActiveChannel.SetName($"Clan {Encoding.UTF8.GetString(tag).Replace("\0", "")}");
        }

        protected void WriteClanInfo()
        {
            if (Users == null) return;
            var message = new SID_CLANINFO();
            var users = Users.ToArray();
            foreach (var (n, r) in users)
            {
                string nStr = Encoding.UTF8.GetString(n);

                if (!Common.GetClientByOnlineName(nStr, out var gameState))
                {
                    continue; // clan member is not online, next
                }

                var arguments = new Dictionary<string, dynamic>() {{ "tag", Tag }, { "rank", r }};
                message.Invoke(new MessageContext(gameState.Client, Protocols.MessageDirection.ServerToClient, arguments));
                gameState.Client.Send(message.ToByteArray(gameState.Client.ProtocolType));
            }
        }

        public void WriteStatusChange(GameState target, bool online)
        {
            var targetUsernameBS = Encoding.UTF8.GetBytes(target.Username);
            if (!GetUserRank(targetUsernameBS, out var rank))
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Clan, "Unable to find target in clan object, cannot write status change without rank");
                return;
            }

            WriteMessageToUsers(new SID_CLANMEMBERSTATUSCHANGE(), new Dictionary<string, dynamic>()
            {
                { "username", target.Username },
                { "rank", rank },
                { "status", (byte)(online ? 1 : 0) },
                { "location", new byte[0] },
            });
        }

        public void WriteMessageToUsers(Message message, Dictionary<string,dynamic> arguments)
        {
            if (Users == null) return;
            var users = Users.ToArray();
            foreach (var (n, _) in users) // discard rank
            {
                string nStr = Encoding.UTF8.GetString(n);

                if (!Common.GetClientByOnlineName(nStr, out var gameState))
                {
                    continue; // clan member is not online, next
                }

                message.Invoke(new MessageContext(gameState.Client, Protocols.MessageDirection.ServerToClient, arguments));
                gameState.Client.Send(message.ToByteArray(gameState.Client.ProtocolType));
            }
        }
    }
}
