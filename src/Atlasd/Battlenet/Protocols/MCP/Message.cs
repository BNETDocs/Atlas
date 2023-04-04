using Atlasd.Battlenet.Protocols.MCP.Messages;
using Atlasd.Battlenet.Exceptions;
using System;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP
{
    abstract class Message
    {
        public byte Id;
        public byte[] Buffer { get; protected set; }

        private const int HEADER_SIZE = 3;

        public static Message FromByteArray(byte[] buffer)
        {
            UInt16 length = (UInt16)((buffer[1] << 8) + buffer[0]);
            byte id = buffer[2];
            byte[] body = new byte[length - HEADER_SIZE];

            System.Buffer.BlockCopy(buffer, HEADER_SIZE, body, 0, length - HEADER_SIZE);

            return FromByteArray(id, body);
        }

        public static Message FromByteArray(byte id, byte[] buffer)
        {
            return ((MessageIds)id) switch
            {
                MessageIds.MCP_NULL         => new MCP_NULL(buffer),
                MessageIds.MCP_STARTUP      => new MCP_STARTUP(buffer),
                MessageIds.MCP_CHARCREATE   => new MCP_CHARCREATE(buffer),
                MessageIds.MCP_CHARLOGON    => new MCP_CHARLOGON(buffer),
                MessageIds.MCP_CHARDELETE   => new MCP_CHARDELETE(buffer),
                MessageIds.MCP_CHARUPGRADE  => new MCP_CHARUPGRADE(buffer),

                MessageIds.MCP_MOTD         => new MCP_MOTD(buffer),

                MessageIds.MCP_CHARLIST     => new MCP_CHARLIST(buffer),
                MessageIds.MCP_CHARLIST2    => new MCP_CHARLIST2(buffer),
                /*
                MessageIds.MCP_ => new MCP_(buffer),
                */
                _ => null,
            };
        }

        public static string MessageName(byte messageId)
        {
            return ((MessageIds)messageId).ToString();
        }

        public byte[] ToByteArray(ProtocolType protocolType)
        {
            if (protocolType.IsGame())
            {
                var size = (UInt16)(HEADER_SIZE + Buffer.Length);
                var buffer = new byte[size];

                buffer[0] = (byte)(size);
                buffer[1] = (byte)(size >> 8);
                buffer[2] = Id;

                System.Buffer.BlockCopy(Buffer, 0, buffer, HEADER_SIZE, Buffer.Length);

                return buffer;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public abstract bool Invoke(MessageContext context);
    }
}
