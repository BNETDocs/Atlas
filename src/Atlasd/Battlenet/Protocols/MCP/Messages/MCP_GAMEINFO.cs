using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_GAMEINFO : Message
    {
        public MCP_GAMEINFO()
        {
            Id = (byte)MessageIds.MCP_GAMEINFO;
            Buffer = new byte[0];
        }

        public MCP_GAMEINFO(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_GAMEINFO;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
