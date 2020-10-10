using Atlasd.Battlenet.Exceptions;
using System;

namespace Atlasd.Battlenet.Protocols.Game
{
    class GameKey
    {
        public enum ProductValues
        {
            Starcraft_A = 0x01,
            Starcraft_B = 0x02,
            WarcraftII = 0x04,
            DiabloIIBeta = 0x05,
            StarcraftIIBeta = 0x05,
            WorldOfWarcraftWrathOfTheLichKingAlpha = 0x05,
            DiabloII_A = 0x06,
            DiabloII_B = 0x07,
            DiabloIIStressTest = 0x09,
            DiabloIILordOfDestruction_A = 0x0A,
            DiabloIILordOfDestruction_Beta = 0x0B,
            DiabloIILordOfDestruction_B = 0x0C,
            WarcraftIIIBeta = 0x0D,
            WarcraftIIIReignOfChaos_A = 0x0E,
            WarcraftIIIReignOfChaos_B = 0x0F,
            WarcraftIIIFrozenThroneBeta = 0x11,
            WarcraftIIIFrozenThrone_A = 0x12,
            WarcraftIIIFrozenThrone_B = 0x13,
            WorldOfWarcraftBurningCrusade = 0x15,
            WorldOfWarcraft14DayTrial = 0x16,
            Starcraft_DigitalDownload = 0x17,
            DiabloII_DigitalDownload = 0x18,
            DiabloIILordOfDestruction_DigitalDownload = 0x19,
            WorldOfWarcraftWrathOfTheLichKing = 0x1A,
            StarcraftII = 0x1C,
            DiabloIII = 0x1D,
            HeroesOfTheStorm = 0x24,
        };

        public byte[] PrivateValue { get; protected set; }
        public UInt32 ProductValue;
        public UInt32 PublicValue;

        public GameKey(UInt32 keyLength, UInt32 productValue, UInt32 publicValue, byte[] hashedKeyData)
        {
            if (!(keyLength == 13 || keyLength == 16 || keyLength == 26))
                throw new ProtocolViolationException(ProtocolType.Game, "Invalid game key length");

            if (!IsValidProductValue(productValue))
                throw new ProtocolViolationException(ProtocolType.Game, "Invalid game key product value");

            SetProductValue(productValue);
            SetPublicValue(publicValue);
            SetPrivateValue(hashedKeyData); // TODO: Do something with hashedKeyData, stuffing it here for now.
        }

        public GameKey(UInt32 productValue, UInt32 publicValue, byte[] privateValue)
        {
            SetPrivateValue(privateValue);
            SetProductValue(productValue);
            SetPublicValue(publicValue);
        }

        public bool IsValidProductValue()
        {
            return ProductValue switch
            {
                (UInt32)ProductValues.DiabloIIBeta => true,
                (UInt32)ProductValues.DiabloIILordOfDestruction_A => true,
                (UInt32)ProductValues.DiabloIILordOfDestruction_B => true,
                (UInt32)ProductValues.DiabloIILordOfDestruction_Beta => true,
                (UInt32)ProductValues.DiabloIILordOfDestruction_DigitalDownload => true,
                (UInt32)ProductValues.DiabloIIStressTest => true,
                (UInt32)ProductValues.DiabloII_A => true,
                (UInt32)ProductValues.DiabloII_B => true,
                (UInt32)ProductValues.DiabloII_DigitalDownload => true,
                (UInt32)ProductValues.Starcraft_A => true,
                (UInt32)ProductValues.Starcraft_B => true,
                (UInt32)ProductValues.Starcraft_DigitalDownload => true,
                (UInt32)ProductValues.WarcraftII => true,
                (UInt32)ProductValues.WarcraftIIIBeta => true,
                (UInt32)ProductValues.WarcraftIIIFrozenThroneBeta => true,
                (UInt32)ProductValues.WarcraftIIIFrozenThrone_A => true,
                (UInt32)ProductValues.WarcraftIIIFrozenThrone_B => true,
                (UInt32)ProductValues.WarcraftIIIReignOfChaos_A => true,
                (UInt32)ProductValues.WarcraftIIIReignOfChaos_B => true,
                _ => false,
            };
        }

        public static bool IsValidProductValue(UInt32 productValue)
        {
            return productValue switch
            {
                (UInt32)ProductValues.DiabloIIBeta => true,
                (UInt32)ProductValues.DiabloIILordOfDestruction_A => true,
                (UInt32)ProductValues.DiabloIILordOfDestruction_B => true,
                (UInt32)ProductValues.DiabloIILordOfDestruction_Beta => true,
                (UInt32)ProductValues.DiabloIILordOfDestruction_DigitalDownload => true,
                (UInt32)ProductValues.DiabloIIStressTest => true,
                (UInt32)ProductValues.DiabloII_A => true,
                (UInt32)ProductValues.DiabloII_B => true,
                (UInt32)ProductValues.DiabloII_DigitalDownload => true,
                (UInt32)ProductValues.Starcraft_A => true,
                (UInt32)ProductValues.Starcraft_B => true,
                (UInt32)ProductValues.Starcraft_DigitalDownload => true,
                (UInt32)ProductValues.WarcraftII => true,
                (UInt32)ProductValues.WarcraftIIIBeta => true,
                (UInt32)ProductValues.WarcraftIIIFrozenThroneBeta => true,
                (UInt32)ProductValues.WarcraftIIIFrozenThrone_A => true,
                (UInt32)ProductValues.WarcraftIIIFrozenThrone_B => true,
                (UInt32)ProductValues.WarcraftIIIReignOfChaos_A => true,
                (UInt32)ProductValues.WarcraftIIIReignOfChaos_B => true,
                _ => false,
            };
        }

        public void SetPrivateValue(byte[] privateValue)
        {
            if (!(privateValue.Length == 4 || privateValue.Length == 20))
                throw new ProtocolViolationException(ProtocolType.Game, "Invalid game key private value");

            PrivateValue = privateValue;
        }

        public void SetProductValue(UInt32 productValue)
        {
            ProductValue = productValue;
        }

        public void SetPublicValue(UInt32 publicValue)
        {
            PublicValue = publicValue;
        }
    }
}
