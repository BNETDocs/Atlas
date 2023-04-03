using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CHARDELETE : Message
    {
        public MCP_CHARDELETE()
        {
            Id = (byte)MessageIds.MCP_CHARDELETE;
            Buffer = new byte[0];
        }

        public MCP_CHARDELETE(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CHARDELETE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
