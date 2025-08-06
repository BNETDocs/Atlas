using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_REQUESTLADDERDATA : Message
    {
        public MCP_REQUESTLADDERDATA()
        {
            Id = (byte)MessageIds.MCP_REQUESTLADDERDATA;
            Buffer = new byte[0];
        }

        public MCP_REQUESTLADDERDATA(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_REQUESTLADDERDATA;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            return false;
        }
    }
}
