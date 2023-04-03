using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CHARUPGRADE : Message
    {
        public MCP_CHARUPGRADE()
        {
            Id = (byte)MessageIds.MCP_CHARUPGRADE;
            Buffer = new byte[0];
        }

        public MCP_CHARUPGRADE(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CHARUPGRADE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
