using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Models
{
    [Flags]
    enum CharacterFlags : byte
    {
        Classic     = 0x00,
        Hardcore    = 0x04,
        Dead        = 0x08,
        Expansion   = 0x20,
        Ladder      = 0x40,
    }
}
