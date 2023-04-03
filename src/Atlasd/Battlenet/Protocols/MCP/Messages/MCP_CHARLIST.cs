using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CHARLIST : Message
    {
        public MCP_CHARLIST()
        {
            Id = (byte)MessageIds.MCP_CHARLIST;
            Buffer = new byte[0];
        }

        public MCP_CHARLIST(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CHARLIST;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
