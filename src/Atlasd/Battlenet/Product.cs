using System;
using System.Linq;
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

        public static ProductCode FromBytes(byte[] product, bool validityCheck)
        {
            if (product.Length != 4)
                throw new ArgumentException($"Cannot convert byte array to product, expected 4 bytes, got {product.Length}");

            ProductCode code = (ProductCode)BitConverter.ToUInt32(product);

            if (validityCheck)
            {
                var codeStr = Encoding.UTF8.GetString(product);
                if (!IsValid(code)) code = (ProductCode)BitConverter.ToUInt32(product.Reverse().ToArray());
                if (!IsValid(code)) code = (ProductCode)BitConverter.ToUInt32(Encoding.UTF8.GetBytes(codeStr.ToUpperInvariant()));
                if (!IsValid(code)) code = (ProductCode)BitConverter.ToUInt32(Encoding.UTF8.GetBytes(codeStr.ToUpperInvariant().Reverse().ToArray()));
                if (!IsValid(code)) code = ProductCode.None;
            }

            return code;
        }

        public static ProductCode FromString(string product, bool validityCheck)
        {
            if (product.Length != 4)
                throw new ArgumentException($"Cannot convert string to product, expected 4 characters, got {product.Length}");

            ProductCode code = (ProductCode)BitConverter.ToUInt32(Encoding.UTF8.GetBytes(product)[0..4]);

            if (validityCheck)
            {
                if (!IsValid(code)) code = (ProductCode)BitConverter.ToUInt32(Encoding.UTF8.GetBytes(product.Reverse().ToString())[0..4]);
                if (!IsValid(code)) code = (ProductCode)BitConverter.ToUInt32(Encoding.UTF8.GetBytes(product.ToUpperInvariant())[0..4]);
                if (!IsValid(code)) code = (ProductCode)BitConverter.ToUInt32(Encoding.UTF8.GetBytes(product.ToUpperInvariant().Reverse().ToString())[0..4]);
                if (!IsValid(code)) code = ProductCode.None;
            }

            return code;
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

        public static bool IsValid(ProductCode code)
        {
            return code switch
            {
                ProductCode.Chat => true,
                ProductCode.DiabloII => true,
                ProductCode.DiabloIILordOfDestruction => true,
                ProductCode.DiabloRetail => true,
                ProductCode.DiabloShareware => true,
                ProductCode.StarcraftBroodwar => true,
                ProductCode.StarcraftJapanese => true,
                ProductCode.StarcraftOriginal => true,
                ProductCode.StarcraftShareware => true,
                ProductCode.WarcraftIIBNE => true,
                ProductCode.WarcraftIIIDemo => true,
                ProductCode.WarcraftIIIFrozenThrone => true,
                ProductCode.WarcraftIIIReignOfChaos => true,
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

        public static byte[] ToByteArray(ProductCode code)
        {
            return BitConverter.GetBytes((uint)code);
        }

        public static string ToString(ProductCode code)
        {
            return Encoding.ASCII.GetString(ToByteArray(code));
        }
    }
}
