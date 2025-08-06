using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_JOINGAME : Message
    {
        public MCP_JOINGAME()
        {
            Id = (byte)MessageIds.MCP_JOINGAME;
            Buffer = new byte[0];
        }

        public MCP_JOINGAME(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_JOINGAME;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
