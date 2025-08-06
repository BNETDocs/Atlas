using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CHARRANK : Message
    {
        public MCP_CHARRANK()
        {
            Id = (byte)MessageIds.MCP_CHARRANK;
            Buffer = new byte[0];
        }

        public MCP_CHARRANK(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CHARRANK;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
