using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.Game.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game
{
    class ChatEvent
    {
        public enum EventIds : UInt32
        {
            EID_USERSHOW = 0x01,
            EID_USERJOIN = 0x02,
            EID_USERLEAVE = 0x03,
            EID_WHISPERFROM = 0x04,
            EID_TALK = 0x05,
            EID_BROADCAST = 0x06,
            EID_CHANNELJOIN = 0x07,
            EID_USERUPDATE = 0x09,
            EID_WHISPERTO = 0x0A,
            EID_CHANNELFULL = 0x0D,
            EID_CHANNELNOTFOUND = 0x0E,
            EID_CHANNELRESTRICTED = 0x0F,
            EID_INFO = 0x12,
            EID_ERROR = 0x13,
            EID_EMOTE = 0x17,
        };

        public EventIds EventId { get; protected set; }
        public UInt32 Flags { get; protected set; }
        public Int32 Ping { get; protected set; }
        public string Username { get; protected set; }
        public string Text { get; protected set; }

        public ChatEvent(EventIds eventId, UInt32 flags, Int32 ping, string username, string text)
        {
            Initialize(eventId, flags, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Account.Flags flags, Int32 ping, string username, string text)
        {
            Initialize(eventId, (UInt32)flags, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Channel.Flags flags, Int32 ping, string username, string text)
        {
            Initialize(eventId, (UInt32)flags, ping, username, text);
        }

        public static string EventIdToString(EventIds eventId)
        {
            return eventId switch {
                EventIds.EID_USERSHOW => "EID_USERSHOW",
                EventIds.EID_USERJOIN => "EID_USERJOIN",
                EventIds.EID_USERLEAVE => "EID_USERLEAVE",
                EventIds.EID_WHISPERFROM => "EID_WHISPERFROM",
                EventIds.EID_TALK => "EID_TALK",
                EventIds.EID_BROADCAST => "EID_BROADCAST",
                EventIds.EID_CHANNELJOIN => "EID_CHANNELJOIN",
                EventIds.EID_USERUPDATE => "EID_USERUPDATE",
                EventIds.EID_WHISPERTO => "EID_WHISPERTO",
                EventIds.EID_CHANNELFULL => "EID_CHANNELFULL",
                EventIds.EID_CHANNELNOTFOUND => "EID_CHANNELNOTFOUND",
                EventIds.EID_CHANNELRESTRICTED => "EID_CHANNELRESTRICTED",
                EventIds.EID_INFO => "EID_INFO",
                EventIds.EID_ERROR => "EID_ERROR",
                EventIds.EID_EMOTE => "EID_EMOTE",
                _ => throw new ArgumentOutOfRangeException(string.Format("Unknown Event Id [0x{0:X8}]", eventId)),
            };
        }

        protected void Initialize(EventIds eventId, UInt32 flags, Int32 ping, string username, string text)
        {
            EventId = eventId;
            Flags = flags;
            Ping = ping;
            Username = username;
            Text = text;
        }

        public byte[] ToByteArray(ProtocolType protocolType)
        {
            switch (protocolType) {
                case ProtocolType.Game:
                    {
                        var buf = new byte[26 + Encoding.ASCII.GetByteCount(Username) + Encoding.UTF8.GetByteCount(Text)];
                        var m = new MemoryStream(buf);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)EventId);
                        w.Write((UInt32)Flags);
                        w.Write((Int32)Ping);
                        w.Write((UInt32)0); // IP address (Defunct)
                        w.Write((UInt32)0xBAADF00D); // Account number (Defunct)
                        w.Write((UInt32)0xDEADBEEF); // Registration authority (Defunct)
                        w.Write((string)Username);

                        w.Write(Encoding.UTF8.GetBytes(Text)); // UTF-8 is needed for localization reasons
                        w.Write((byte)0);

                        w.Close();
                        m.Close();

                        return buf;
                    }
                case ProtocolType.Chat:
                case ProtocolType.Chat_Alt1:
                case ProtocolType.Chat_Alt2:
                    {
                        var buf = $"{1000 + EventId} ";
                        var product = Text.Length < 4 ? "" : Text[0..4];

                        switch (EventId)
                        {
                            case EventIds.EID_USERSHOW:
                            case EventIds.EID_USERUPDATE:
                                {
                                    buf += $"USER {Username} {Flags:X4} [{product}]";
                                    break;
                                }
                            case EventIds.EID_USERJOIN:
                                {
                                    buf += $"JOIN {Username} {Flags:X4} [{product}]";
                                    break;
                                }
                            case EventIds.EID_USERLEAVE:
                                {
                                    buf += $"LEAVE {Username} {Flags:X4}";
                                    break;
                                }
                            case EventIds.EID_WHISPERFROM:
                            case EventIds.EID_WHISPERTO:
                                {
                                    buf += $"WHISPER {Username} {Flags:X4} \"{Text}\"";
                                    break;
                                }
                            case EventIds.EID_TALK:
                                {
                                    buf += $"TALK {Username} {Flags:X4} \"{Text}\"";
                                    break;
                                }
                            case EventIds.EID_BROADCAST:
                                {
                                    buf += $"BROADCAST \"{Text}\"";
                                    break;
                                }
                            case EventIds.EID_CHANNELJOIN:
                                {
                                    buf += $"CHANNEL \"{Text}\"";
                                    break;
                                }
                            case EventIds.EID_INFO:
                                {
                                    buf += $"INFO \"{Text}\"";
                                    break;
                                }
                            case EventIds.EID_ERROR:
                                {
                                    buf += $"ERROR \"{Text}\"";
                                    break;
                                }
                            case EventIds.EID_EMOTE:
                                {
                                    buf += $"EMOTE {Username} {Flags:X4} \"{Text}\"";
                                    break;
                                }
                            default:
                                {
                                    buf += $"UNKNOWN {Username} {Flags:X4} \"{Text}\"";
                                    break;
                                }
                        }

                        buf += "\r\n";
                        return Encoding.ASCII.GetBytes(buf);
                    }
                default:
                    throw new ProtocolNotSupportedException(protocolType, null, $"Unsupported protocol type [0x{(byte)protocolType:X2}]");
            }
        }

        public void WriteTo(ClientState receiver)
        {
            WriteTo(this, receiver);
        }

        public static void WriteTo(ChatEvent chatEvent, ClientState receiver)
        {
            var args = new Dictionary<string, object> {{ "chatEvent", chatEvent }};
            var msg = new SID_CHATEVENT();

            msg.Invoke(new MessageContext(receiver, MessageDirection.ServerToClient, args));

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
                        receiver.Send(Encoding.UTF8.GetBytes(msg.ToString()));
                        break;
                    }
                default:
                    throw new NotSupportedException("Invalid channel state, user in channel is using an incompatible protocol");
            }
        }
    }
}
