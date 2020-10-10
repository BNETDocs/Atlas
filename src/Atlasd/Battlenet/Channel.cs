using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet
{
    class Channel
    {
        public enum Flags : UInt32
        {
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
        public int MaxUsers { get; protected set; }
        public string Name { get; protected set; }
        public string Topic { get; protected set; }
        protected List<GameState> Users { get; private set; }
    
        public Channel(string name, Flags flags, int maxUsers = -1, string topic = "")
        {
            ActiveFlags = flags;
            MaxUsers = maxUsers;
            Name = name;
            Topic = topic;
            Users = new List<GameState>();
        }

        public void AcceptUser(GameState user)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Channel, "Accepting user [" + user.OnlineName + "] to [" + Name + "]");

            // Add this user to the channel:
            Users.Add(user);

            // Tell this user they entered the channel:
            WriteChatEvent(user.Client, SID_CHATEVENT.EventIds.EID_CHANNELJOIN, 0, 0, "", Name);

            foreach (var subuser in Users)
            {
                // Tell this user about everyone in the channel:
                WriteChatEvent(user.Client, SID_CHATEVENT.EventIds.EID_USERSHOW, 0, subuser.Ping, subuser.OnlineName, Encoding.ASCII.GetString(subuser.Statstring));

                // Tell everyone else about this user entering the channel:
                if (subuser != user)
                    WriteChatEvent(subuser.Client, SID_CHATEVENT.EventIds.EID_USERJOIN, 0, user.Ping, user.OnlineName, Encoding.ASCII.GetString(user.Statstring));
            }

            // Render the channel topic:
            var rendered_topic = Topic;

            rendered_topic.Replace("%channel.name%", Name);
            rendered_topic.Replace("%channel.maxusers%", MaxUsers.ToString());
            rendered_topic.Replace("%channel.users%", Users.Count.ToString());
            rendered_topic.Replace("%user.account%", (string)user.ActiveAccount.Get(Account.UsernameKey));
            rendered_topic.Replace("%user.game%", Product.ProductName(user.Product, false));
            rendered_topic.Replace("%user.gamelong%", Product.ProductName(user.Product, true));
            rendered_topic.Replace("%user.name%", user.OnlineName);
            rendered_topic.Replace("%user.ping%", user.Ping.ToString() + "ms");

            foreach (var line in rendered_topic.Split("\n"))
                WriteChatEvent(SID_CHATEVENT.EventIds.EID_INFO, 0, 0, "", line);
        }

        public static Channel GetChannelByName(string name)
        {
            return Common.ActiveChannels.TryGetValue(name, out Channel channel) ? channel : null;
        }

        public void RemoveUser(GameState user)
        {
            if (Users.Contains(user)) Users.Remove(user);

            foreach (var subuser in Users)
            {
                // Tell everyone else about this user leaving the channel:
                WriteChatEvent(subuser.Client, SID_CHATEVENT.EventIds.EID_USERLEAVE, 0, user.Ping, user.OnlineName, Encoding.ASCII.GetString(user.Statstring));
            }
        }

        public void Resync()
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Server, "[TODO] Channel.Resync()");
        }

        public void SetActiveFlags(Flags newFlags)
        {
            ActiveFlags = newFlags;
            Resync();
        }

        public void SetMaxUsers(int maxUsers)
        {
            MaxUsers = maxUsers;
        }

        public void SetName(string newName)
        {
            Name = newName;
            Resync();
        }

        public void SetTopic(string newTopic)
        {
            Topic = newTopic;
            WriteChatInfo("The channel topic was updated by Anonymous:");
            WriteChatInfo(newTopic);
        }

        public void TryAcceptUser(GameState user)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Server, "[TODO] Channel.TryAcceptUser()");
            AcceptUser(user);
        }

        public void WriteChatError(string text)
        {
            WriteChatEvent(SID_CHATEVENT.EventIds.EID_ERROR, 0, 0, "", text);
        }

        public void WriteChatInfo(string text)
        {
            WriteChatEvent(SID_CHATEVENT.EventIds.EID_INFO, 0, 0, "", text);
        }

        public static void WriteChatEvent(Sockets.ClientState client, SID_CHATEVENT.EventIds eventId, UInt32 flags, Int32 ping, string username, string text)
        {
            if (client.ClientStream == null || !client.ClientStream.CanWrite)
            {
                client.Close();
                return;
            }

            var args = new Dictionary<string, object>
            {
                { "eventId", (UInt32)eventId },
                { "flags", flags },
                { "ping", ping },
                { "username", username },
                { "text", text }
            };

            var chat_event = new SID_CHATEVENT();
            chat_event.Invoke(new MessageContext(client, Protocols.MessageDirection.ServerToClient, args));

            switch (client.ProtocolType)
            {
                case ProtocolType.Game:
                    {
                        client.Send(chat_event.ToByteArray());
                        break;
                    }
                case ProtocolType.Chat:
                case ProtocolType.Chat_Alt1:
                case ProtocolType.Chat_Alt2:
                    {
                        client.Send(Encoding.ASCII.GetBytes(chat_event.ToString()));
                        break;
                    }
                default:
                    {
                        throw new NotSupportedException("User is using an incompatible protocol for chat events");
                    }
            }
        }

        protected void WriteChatEvent(SID_CHATEVENT.EventIds eventId, UInt32 flags, Int32 ping, string username, string text)
        {
            var args = new Dictionary<string, object>
            {
                { "eventId", (UInt32)eventId },
                { "flags", flags },
                { "ping", ping },
                { "username", username },
                { "text", text }
            };

            var chat_event = new SID_CHATEVENT();

            foreach (var user in Users)
            {
                chat_event.Invoke(new MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args));

                switch (user.Client.ProtocolType)
                {
                    case ProtocolType.Game:
                        {
                            user.Client.Send(chat_event.ToByteArray());
                            break;
                        }
                    case ProtocolType.Chat:
                    case ProtocolType.Chat_Alt1:
                    case ProtocolType.Chat_Alt2:
                        {
                            user.Client.Send(Encoding.ASCII.GetBytes(chat_event.ToString()));
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
