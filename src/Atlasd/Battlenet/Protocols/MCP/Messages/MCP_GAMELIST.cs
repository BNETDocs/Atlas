using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_GAMELIST : Message
    {
        public MCP_GAMELIST()
        {
            Id = (byte)MessageIds.MCP_GAMELIST;
            Buffer = new byte[0];
        }

        public MCP_GAMELIST(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_GAMELIST;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
