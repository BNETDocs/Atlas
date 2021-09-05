using System;
using System.Text;

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

            WarcraftIIBNE = 0x5732424E, // W2BN
            WarcraftIIIDemo = 0x5733444D, // W3DM
            WarcraftIIIFrozenThrone = 0x57335850, // W3XP
            WarcraftIIIReignOfChaos = 0x57415233, // WAR3
        }

        public static bool IsChatRestricted(ProductCode code)
        {
            return code switch
            {
                ProductCode.Chat => true,
                ProductCode.DiabloRetail => true,
                ProductCode.DiabloShareware => true,
                ProductCode.StarcraftShareware => true,
                ProductCode.WarcraftIIIDemo => true,
                _ => false,
            };
        }

        public static bool IsChat(ProductCode code)
        {
            return code == ProductCode.Chat;
        }

        public static bool IsDiablo(ProductCode code)
        {
            return code switch
            {
                ProductCode.DiabloRetail => true,
                ProductCode.DiabloShareware => true,
                _ => false,
            };
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

        public static bool IsStarcraft(ProductCode code)
        {
            return code switch
            {
                ProductCode.StarcraftBroodwar => true,
                ProductCode.StarcraftJapanese => true,
                ProductCode.StarcraftOriginal => true,
                ProductCode.StarcraftShareware => true,
                _ => false,
            };
        }

        public static bool IsUDPSupported(ProductCode code)
        {
            return code switch
            {
                ProductCode.DiabloRetail => true,
                ProductCode.DiabloShareware => true,
                ProductCode.StarcraftBroodwar => true,
                ProductCode.StarcraftJapanese => true,
                ProductCode.StarcraftOriginal => true,
                ProductCode.StarcraftShareware => true,
                ProductCode.WarcraftIIBNE => true,
                _ => false,
            };
        }

        public static bool IsWarcraftII(ProductCode code)
        {
            return code == ProductCode.WarcraftIIBNE;
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
                ProductCode.DiabloIILordOfDestruction => "Diablo II " + (extended ? "Lord of Destruction" : "LoD"),
                ProductCode.DiabloRetail              => "Diablo",
                ProductCode.DiabloShareware           => "Diablo Shareware",
                ProductCode.StarcraftBroodwar         => "Starcraft Broodwar",
                ProductCode.StarcraftJapanese         => "Starcraft Japanese",
                ProductCode.StarcraftOriginal         => "Starcraft Original",
                ProductCode.StarcraftShareware        => "Starcraft Shareware",
                ProductCode.WarcraftIIBNE             => "Warcraft II " + (extended ? "Battle.net Edition" : "BNE"),
                ProductCode.WarcraftIIIDemo           => "Warcraft III Demo",
                ProductCode.WarcraftIIIFrozenThrone   => "Warcraft III " + (extended ? "The Frozen Throne" : "TFT"),
                ProductCode.WarcraftIIIReignOfChaos   => "Warcraft III " + (extended ? "Reign of Chaos" : "RoC"),
                _ => "Unknown" + (extended ? " (" + code.ToString() + ")" : ""),
            };
        }

        public static string ProductChannelName(ProductCode code)
        {
            return code switch
            {
                ProductCode.Chat => "Public Chat",
                ProductCode.DiabloII => "Diablo II",
                ProductCode.DiabloIILordOfDestruction => "Lord of Destruction",
                ProductCode.DiabloRetail => "Diablo",
                ProductCode.DiabloShareware => "Diablo Shareware",
                ProductCode.StarcraftBroodwar => "Brood War",
                ProductCode.StarcraftJapanese => "StarCraft",
                ProductCode.StarcraftOriginal => "StarCraft",
                ProductCode.StarcraftShareware => "StarCraft Shareware",
                ProductCode.WarcraftIIBNE => "WarCraft II",
                ProductCode.WarcraftIIIDemo => "WarCraft III",
                ProductCode.WarcraftIIIFrozenThrone => "Frozen Throne",
                ProductCode.WarcraftIIIReignOfChaos => "WarCraft III",
                _ => "Unknown",
            };
        }

        public static ProductCode StringToProduct(string product)
        {
            switch (product.ToUpper())
            {
                case "CHAT":
                case "TAHC":
                    return ProductCode.Chat;
                case "D2DV":
                case "VD2D":
                    return ProductCode.DiabloII;
                case "D2XP":
                case "PX2D":
                    return ProductCode.DiabloIILordOfDestruction;
                case "DRTL":
                case "LTRD":
                    return ProductCode.DiabloRetail;
                case "DSHR":
                case "RHSD":
                    return ProductCode.DiabloShareware;
                case "SEXP":
                case "PXES":
                    return ProductCode.StarcraftBroodwar;
                case "JSTR":
                case "RTSJ":
                    return ProductCode.StarcraftJapanese;
                case "STAR":
                case "RATS":
                    return ProductCode.StarcraftOriginal;
                case "SSHR":
                case "RHSS":
                    return ProductCode.StarcraftShareware;
                case "W2BN":
                case "NB2W":
                    return ProductCode.WarcraftIIBNE;
                case "W3DM":
                case "MD3W":
                    return ProductCode.WarcraftIIIDemo;
                case "W3XP":
                case "PX3W":
                    return ProductCode.WarcraftIIIFrozenThrone;
                case "WAR3":
                case "3RAW":
                    return ProductCode.WarcraftIIIReignOfChaos;
                default:
                    return ProductCode.None;
            }
        }
    }
}
