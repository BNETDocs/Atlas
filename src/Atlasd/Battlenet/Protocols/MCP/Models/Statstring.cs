using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.MCP.Models
{
    class Statstring
    {
        public byte Unknown_1 { get; set; }
        public byte Unknown_2 { get; set; }
        public byte Head { get; set; }
        public byte Torso { get; set; }
        public byte Legs { get; set; }
        public byte RightArm { get; set; }
        public byte LeftArm { get; set; }
        public byte RightWeapon { get; set; }
        public byte LeftWeapon { get; set; }
        public byte Shield { get; set; }
        public byte RightShoulder { get; set; }
        public byte LeftShoulder { get; set; }
        public byte LeftItem { get; set; }
        public byte Type { get; set; }
        public byte ColorHead { get; set; }
        public byte ColorTorso { get; set; }
        public byte ColorLegs { get; set; }
        public byte ColorRightArm { get; set; }
        public byte ColorLeftArm { get; set; }
        public byte ColorRightWeapon { get; set; }
        public byte ColorLeftWeapon { get; set; }
        public byte ColorShield { get; set; }
        public byte ColorRightShoulder { get; set; }
        public byte ColorLeftShoulder { get; set; }
        public byte ColorLeftItem { get; set; }
        public byte Level { get; set; }
        public byte Flags { get; set; }
        public byte Act { get; set; }
        public byte Unknown_3 { get; set; }
        public byte Unknown_4 { get; set; }
        public byte Ladder { get; set; }
        public byte Unknown_5 { get; set; }
        public byte Unknown_6 { get; set; }

        public Statstring(CharacterTypes type, CharacterFlags flags, LadderTypes ladder)
        {
            Unknown_1           = 0x84;
            Unknown_2           = 0x80;
            Head                = 0xFF;
            Torso               = 0xFF;
            Legs                = 0xFF;
            RightArm            = 0xFF;
            LeftArm             = 0xFF;
            RightWeapon         = getRightWeapon(type);
            LeftWeapon          = 0xFF;
            Shield              = getShield(type);
            RightShoulder       = 0xFF;
            LeftShoulder        = 0xFF;
            LeftItem            = 0xFF;
            Type                = (byte)type;
            ColorHead           = 0xFF;
            ColorTorso          = 0xFF;
            ColorLegs           = 0xFF;
            ColorRightArm       = 0xFF;
            ColorLeftArm        = 0xFF;
            ColorRightWeapon    = 0xFF;
            ColorLeftWeapon     = 0xFF;
            ColorShield         = 0xFF;
            ColorRightShoulder  = 0xFF;
            ColorLeftShoulder   = 0xFF;
            ColorLeftItem       = 0xFF;
            Level               = 0x01; // 1
            Flags               = (byte)flags;
            Act                 = 0x80; // normal act 1
            Unknown_3           = 0xFF; // i think this field is documented incorrectly (0x80 = never logged in, 0xFF = has logged in)
            Unknown_4           = 0xFF; // i think this field is documented incorrectly (0x80 = never logged in, 0xFF = has logged in)
            Ladder              = (byte)ladder;
            Unknown_5           = 0xFF;
            Unknown_6           = 0xFF;
        }

        private byte getRightWeapon(CharacterTypes type)
        {
            switch (type)
            {
                case CharacterTypes.Amazon:         return 0x1B;
                case CharacterTypes.Sorceress:      return 0x25;
                case CharacterTypes.Necromancer:    return 0x09;
                case CharacterTypes.Paladin:        return 0x11;
                case CharacterTypes.Barbarian:      return 0x04;
                case CharacterTypes.Druid:          return 0x0C;
                case CharacterTypes.Assassin:       return 0x2D;
            }

            return 0xFF;
        }

        private byte getShield(CharacterTypes type)
        {
            switch (type)
            {
                case CharacterTypes.Amazon:
                case CharacterTypes.Paladin:
                case CharacterTypes.Barbarian:
                case CharacterTypes.Druid:
                case CharacterTypes.Assassin:
                    return 0x4F;
            }

            return 0xFF;
        }

        public byte[] ToBytes()
        {
            return new byte[]
            {
                Unknown_1,
                Unknown_2,
                Head,
                Torso,
                Legs,
                RightArm,
                LeftArm,
                RightWeapon,
                LeftWeapon,
                Shield,
                RightShoulder,
                LeftShoulder,
                LeftItem,
                Type,
                ColorHead,
                ColorTorso,
                ColorLegs,
                ColorRightArm,
                ColorLeftArm,
                ColorRightWeapon,
                ColorLeftWeapon,
                ColorShield,
                ColorRightShoulder,
                ColorLeftShoulder,
                ColorLeftItem,
                Level,
                Flags,
                Act,
                Unknown_3,
                Unknown_4,
                Ladder,
                Unknown_5,
                Unknown_6,
            };
        }
    }
}
