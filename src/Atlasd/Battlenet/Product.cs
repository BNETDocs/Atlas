using System;

namespace Atlasd.Battlenet
{
    public class Product
    {
        public enum ProductCode : UInt32
        {
            None = 0, // None/Zero/Null

            Chat = 0x43484154, // CHAT

            DiabloII = 0x44324456, // D2DV
            DiabloIILordOfDestruction = 0x44325850, // D2XP
            DiabloRetail = 0x4452544C, // DRTL
            DiabloShareware = 0x44534852, // DSHR

            StarcraftBroodwar = 0x53455850, // SEXP
            StarcraftJapanese = 0x4A535452, // JSTR
            StarcraftOriginal = 0x53544152, // STAR
            StarcraftShareware = 0x53534852, // SSHR

            WarcraftII = 0x5732424E, // W2BN
            WarcraftIIIDemo = 0x5733444D, // W3DM
            WarcraftIIIFrozenThrone = 0x57335850, // W3XP
            WarcraftIIIReignOfChaos = 0x57415233, // WAR3
        }

        public static bool IsDiabloII(ProductCode code)
        {
            return code switch
            {
                ProductCode.DiabloII => true,
                ProductCode.DiabloIILordOfDestruction => true,
                _ => false,
            };
        }

        public static bool IsWarcraftIII(ProductCode code)
        {
            return code switch
            {
                ProductCode.WarcraftIIIDemo => true,
                ProductCode.WarcraftIIIReignOfChaos => true,
                ProductCode.WarcraftIIIFrozenThrone => true,
                _ => false,
            };
        }

        public static string ProductName(ProductCode code, bool extended = true)
        {
            return code switch
            {
                ProductCode.None                      => "None",
                ProductCode.Chat                      => "Chat",
                ProductCode.DiabloII                  => "Diablo II",
                ProductCode.DiabloIILordOfDestruction => "Diablo II " + (extended ? " Lord of Destruction" : " LoD"),
                ProductCode.DiabloRetail              => "Diablo",
                ProductCode.DiabloShareware           => "Diablo Shareware",
                ProductCode.StarcraftBroodwar         => "Starcraft Broodwar",
                ProductCode.StarcraftJapanese         => "Starcraft Japanese",
                ProductCode.StarcraftOriginal         => "Starcraft Original",
                ProductCode.StarcraftShareware        => "Starcraft Shareware",
                ProductCode.WarcraftII                => "Warcraft II" + (extended ? " Battle.net Edition" : " BNE"),
                ProductCode.WarcraftIIIDemo           => "Warcraft III Demo",
                ProductCode.WarcraftIIIFrozenThrone   => "Warcraft III" + (extended ? " The Frozen Throne" : " TFT"),
                ProductCode.WarcraftIIIReignOfChaos   => "Warcraft III" + (extended ? " Reign of Chaos" : " RoC"),
                _ => "Unknown" + (extended ? " (" + code.ToString() + ")" : ""),
            };
        }
    }
}
