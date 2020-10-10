using Atlasd.Battlenet.Exceptions;
using System;

namespace Atlasd.Battlenet.Protocols.Game
{
    abstract class Message
    {
        public byte Id;
        public byte[] Buffer { get; protected set; }

        public static Message FromByteArray(byte[] buffer)
        {
            if (buffer[0] != 0xFF)
                throw new ProtocolViolationException(ProtocolType.Game, "Invalid message header");

            byte id = buffer[1];
            UInt16 length = (UInt16)((buffer[3] << 8) + buffer[2]);
            byte[] body = new byte[length - 4];

            System.Buffer.BlockCopy(buffer, 4, body, 0, length - 4);

            return FromByteArray(id, body);
        }

        public static Message FromByteArray(byte id, byte[] buffer)
        {
            return ((MessageIds)id) switch
            {
                MessageIds.SID_NULL => new Messages.SID_NULL(buffer),
                MessageIds.SID_ENTERCHAT => new Messages.SID_ENTERCHAT(buffer),
                MessageIds.SID_GETCHANNELLIST => new Messages.SID_GETCHANNELLIST(buffer),
                MessageIds.SID_JOINCHANNEL => new Messages.SID_JOINCHANNEL(buffer),
                MessageIds.SID_PING => new Messages.SID_PING(buffer),
                MessageIds.SID_LOGONRESPONSE2 => new Messages.SID_LOGONRESPONSE2(buffer),
                MessageIds.SID_CREATEACCOUNT2 => new Messages.SID_CREATEACCOUNT2(buffer),
                MessageIds.SID_AUTH_INFO => new Messages.SID_AUTH_INFO(buffer),
                MessageIds.SID_AUTH_CHECK => new Messages.SID_AUTH_CHECK(buffer),
                _ => null,
            };
        }

        public byte[] ToByteArray()
        {
            var size = (UInt16)(4 + Buffer.Length);
            var buffer = new byte[size];

            buffer[0] = 0xFF;
            buffer[1] = Id;
            buffer[2] = (byte)(size);
            buffer[3] = (byte)(size >> 8);

            System.Buffer.BlockCopy(Buffer, 0, buffer, 4, Buffer.Length);

            return buffer;
        }

        public abstract bool Invoke(MessageContext context);
    }
}
