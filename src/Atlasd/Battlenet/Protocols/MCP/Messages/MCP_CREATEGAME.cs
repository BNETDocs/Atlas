using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CREATEGAME : Message
    {
        public MCP_CREATEGAME()
        {
            Id = (byte)MessageIds.MCP_CREATEGAME;
            Buffer = new byte[0];
        }

        public MCP_CREATEGAME(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CREATEGAME;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
