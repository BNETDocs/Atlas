using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CHATEVENT : Message
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

        public SID_CHATEVENT()
        {
            Id = (byte)MessageIds.SID_CHATEVENT;
            Buffer = new byte[4];
        }

        public SID_CHATEVENT(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CHATEVENT;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction == MessageDirection.ClientToServer)
                throw new ProtocolViolationException(ProtocolType.Game, "Client isn't allowed to send SID_CHATEVENT");

            context.Arguments.TryGetValue("username", out object username);
            context.Arguments.TryGetValue("text", out object text);

            if (username == null) username = "";
            if (text == null) text = "";

            Buffer = new byte[26 + ((string)username).Length + ((string)text).Length];

            var m = new MemoryStream(Buffer);
            var w = new BinaryWriter(m);

            w.Write((UInt32)context.Arguments["eventId"]);
            w.Write((UInt32)context.Arguments["flags"]);
            w.Write((Int32)context.Arguments["ping"]);
            w.Write((UInt32)0); // IP Address
            w.Write((UInt32)0xBAADF00D); // Account number
            w.Write((UInt32)0xDEADBEEF); // Registration authority
            w.Write((string)context.Arguments["username"]);
            w.Write((string)context.Arguments["text"]);

            w.Close();
            m.Close();

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_CHATEVENT (" + (4 + Buffer.Length) + " bytes)");

            return true;
        }
    }
}
