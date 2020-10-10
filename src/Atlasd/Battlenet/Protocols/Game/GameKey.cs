using Atlasd.Battlenet.Exceptions;
using System;

namespace Atlasd.Battlenet.Protocols.Game
{
    class GameKey
    {
        public enum ProductValues : UInt32
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

            if (!IsValidProductValue((ProductValues)productValue))
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
            return GameKey.IsValidProductValue((ProductValues)ProductValue);
        }

        public static bool IsValidProductValue(ProductValues productValue)
        {
            return productValue switch
            {
                ProductValues.DiabloIIBeta => true,
                ProductValues.DiabloIILordOfDestruction_A => true,
                ProductValues.DiabloIILordOfDestruction_B => true,
                ProductValues.DiabloIILordOfDestruction_Beta => true,
                ProductValues.DiabloIILordOfDestruction_DigitalDownload => true,
                ProductValues.DiabloIIStressTest => true,
                ProductValues.DiabloII_A => true,
                ProductValues.DiabloII_B => true,
                ProductValues.DiabloII_DigitalDownload => true,
                ProductValues.Starcraft_A => true,
                ProductValues.Starcraft_B => true,
                ProductValues.Starcraft_DigitalDownload => true,
                ProductValues.WarcraftII => true,
                ProductValues.WarcraftIIIBeta => true,
                ProductValues.WarcraftIIIFrozenThroneBeta => true,
                ProductValues.WarcraftIIIFrozenThrone_A => true,
                ProductValues.WarcraftIIIFrozenThrone_B => true,
                ProductValues.WarcraftIIIReignOfChaos_A => true,
                ProductValues.WarcraftIIIReignOfChaos_B => true,
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
