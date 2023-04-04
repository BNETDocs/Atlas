using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_NULL : Message
    {
        public MCP_NULL()
        {
            Id = (byte)MessageIds.MCP_NULL;
            Buffer = new byte[0];
        }

        public MCP_NULL(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_NULL;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
