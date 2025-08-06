using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP
{
    enum CharacterTypes : byte
    {
        Amazon      = 0x01,
        Sorceress   = 0x02,
        Necromancer = 0x03,
        Paladin     = 0x04,
        Barbarian   = 0x05,
        Druid       = 0x06,
        Assassin    = 0x07,
    }
}
