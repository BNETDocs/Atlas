using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
        public IPAddress IPAddress { get; protected set; }
        public Int32 Ping { get; protected set; }
        public string Username { get; protected set; }
        public byte[] Text { get; protected set; }

        public ChatEvent(EventIds eventId, UInt32 flags, Int32 ping, string username, byte[] text)
        {
            Initialize(eventId, flags, null, ping, username, text);
        }

        public ChatEvent(EventIds eventId, UInt32 flags, Int32 ping, string username, string text)
        {
            Initialize(eventId, flags, null, ping, username, text);
        }

        public ChatEvent(EventIds eventId, UInt32 flags, IPAddress ipAddress, Int32 ping, string username, byte[] text)
        {
            Initialize(eventId, flags, ipAddress, ping, username, text);
        }

        public ChatEvent(EventIds eventId, UInt32 flags, IPAddress ipAddress, Int32 ping, string username, string text)
        {
            Initialize(eventId, flags, ipAddress, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Account.Flags flags, Int32 ping, string username, byte[] text)
        {
            Initialize(eventId, (UInt32)flags, null, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Account.Flags flags, Int32 ping, string username, string text)
        {
            Initialize(eventId, (UInt32)flags, null, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Account.Flags flags, IPAddress ipAddress, Int32 ping, string username, byte[] text)
        {
            Initialize(eventId, (UInt32)flags, ipAddress, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Account.Flags flags, IPAddress ipAddress, Int32 ping, string username, string text)
        {
            Initialize(eventId, (UInt32)flags, ipAddress, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Channel.Flags flags, Int32 ping, string username, byte[] text)
        {
            Initialize(eventId, (UInt32)flags, null, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Channel.Flags flags, Int32 ping, string username, string text)
        {
            Initialize(eventId, (UInt32)flags, null, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Channel.Flags flags, IPAddress ipAddress, Int32 ping, string username, byte[] text)
        {
            Initialize(eventId, (UInt32)flags, ipAddress, ping, username, text);
        }

        public ChatEvent(EventIds eventId, Channel.Flags flags, IPAddress ipAddress, Int32 ping, string username, string text)
        {
            Initialize(eventId, (UInt32)flags, ipAddress, ping, username, text);
        }

        public static bool EventIdIsChatMessage(EventIds eventId)
        {
            return eventId switch {
                EventIds.EID_WHISPERFROM => true,
                EventIds.EID_TALK => true,
                EventIds.EID_BROADCAST => true,
                EventIds.EID_WHISPERTO => true,
                EventIds.EID_INFO => true,
                EventIds.EID_ERROR => true,
                EventIds.EID_EMOTE => true,
                _ => false,
            };
        }

        public static string EventIdToString(EventIds eventId)
        {
            return eventId switch
            {
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

        protected void Initialize(EventIds eventId, UInt32 flags, IPAddress ipAddress, Int32 ping, string username, byte[] text)
        {
            EventId = eventId;
            Flags = flags;
            IPAddress = ipAddress;
            Ping = ping;
            Username = username;
            Text = text;
        }

        protected void Initialize(EventIds eventId, UInt32 flags, IPAddress ipAddress, Int32 ping, string username, string text)
        {
            Initialize(eventId, flags, ipAddress, ping, username, Encoding.UTF8.GetBytes(text));
        }

        public byte[] ToByteArray(ProtocolType.Types protocolType)
        {
            var enableIPAddressInChatEvents = Settings.GetBoolean(new string[] { "battlenet", "emulation", "enable_ip_address_in_chatevents" }, false);

            switch (protocolType) {
                case ProtocolType.Types.Game:
                    {
                        var buf = new byte[26 + Encoding.UTF8.GetByteCount(Username) + Text.Length];
                        using var m = new MemoryStream(buf);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)EventId);
                        w.Write((UInt32)Flags);
                        w.Write((Int32)Ping);
                        if (!enableIPAddressInChatEvents || IPAddress == null) w.Write((UInt32)0); else w.Write(IPAddress.MapToIPv4().GetAddressBytes());
                        w.Write((UInt32)0xBAADF00D); // Account number (Defunct)
                        w.Write((UInt32)0xBAADF00D); // Registration authority (Defunct)
                        w.Write((string)Username);
                        w.WriteByteString(Text);

                        return buf;
                    }
                case ProtocolType.Types.Chat:
                case ProtocolType.Types.Chat_Alt1:
                case ProtocolType.Types.Chat_Alt2:
                    {
                        var buf = $"{1000 + EventId} ";
                        var product = new byte[4];

                        Buffer.BlockCopy(Text, 0, product, 0, Math.Min(4, Text.Length));
                        Array.Reverse(product); // "RATS" becomes "STAR", "STAR" becomes "RATS", etc.

                        switch (EventId)
                        {
                            case EventIds.EID_USERSHOW:
                            case EventIds.EID_USERUPDATE:
                                {
                                    buf += $"USER {Username} {Flags:X4} [{Encoding.UTF8.GetString(product)}]";
                                    break;
                                }
                            case EventIds.EID_USERJOIN:
                                {
                                    buf += $"JOIN {Username} {Flags:X4} [{Encoding.UTF8.GetString(product)}]";
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
                                    buf += $"WHISPER {Username} {Flags:X4} \"{Encoding.UTF8.GetString(Text)}\"";
                                    break;
                                }
                            case EventIds.EID_TALK:
                                {
                                    buf += $"TALK {Username} {Flags:X4} \"{Encoding.UTF8.GetString(Text)}\"";
                                    break;
                                }
                            case EventIds.EID_BROADCAST:
                                {
                                    buf += $"BROADCAST \"{Encoding.UTF8.GetString(Text)}\"";
                                    break;
                                }
                            case EventIds.EID_CHANNELJOIN:
                                {
                                    buf += $"CHANNEL \"{Encoding.UTF8.GetString(Text)}\"";
                                    break;
                                }
                            case EventIds.EID_INFO:
                                {
                                    buf += $"INFO \"{Encoding.UTF8.GetString(Text)}\"";
                                    break;
                                }
                            case EventIds.EID_ERROR:
                                {
                                    buf += $"ERROR \"{Encoding.UTF8.GetString(Text)}\"";
                                    break;
                                }
                            case EventIds.EID_EMOTE:
                                {
                                    buf += $"EMOTE {Username} {Flags:X4} \"{Encoding.UTF8.GetString(Text)}\"";
                                    break;
                                }
                            default:
                                {
                                    buf += $"UNKNOWN {Username} {Flags:X4} \"{Encoding.UTF8.GetString(Text)}\"";
                                    break;
                                }
                        }

                        buf += Battlenet.Common.NewLine;
                        return Encoding.UTF8.GetBytes(buf);
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
            receiver.Send(msg.ToByteArray(receiver.ProtocolType));
        }
    }
}
