using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Models
{
    class Character
    {
        public string Name { get; private set; }
        public CharacterTypes Type { get; private set; }
        public CharacterFlags Flags { get; set; }
        public LadderTypes Ladder { get; private set; }
        public Statstring Statstring { get; private set; }

        public Character(string name, CharacterTypes type, CharacterFlags flags, LadderTypes ladder)
        {
            Name = name;
            Type = type;
            Flags = flags;
            Ladder = ladder;

            Statstring = new Statstring(type, flags, ladder);
        }
    }
}
