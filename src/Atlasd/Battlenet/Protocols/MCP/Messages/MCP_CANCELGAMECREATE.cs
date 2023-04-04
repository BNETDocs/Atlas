using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CANCELGAMECREATE : Message
    {
        public MCP_CANCELGAMECREATE()
        {
            Id = (byte)MessageIds.MCP_CANCELGAMECREATE;
            Buffer = new byte[0];
        }

        public MCP_CANCELGAMECREATE(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CANCELGAMECREATE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
