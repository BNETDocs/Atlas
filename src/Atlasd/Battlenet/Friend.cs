using System;

namespace Atlasd.Battlenet
{
    class Friend
    {
        public enum Location : byte
        {
            Offline = 0x00,
            NotInChat = 0x01,
            InChat = 0x02,
            InPublicGame = 0x03,
            InPrivateGame = 0x04,
            InPasswordGame = 0x05,
        };

        [Flags]
        public enum Status : byte
        {
            None = 0x00,
            Mutual = 0x01,
            DoNotDisturb = 0x02,
            Away = 0x04,
        };

        public string Username { get; private set; }

        public Friend(string username)
        {
            Username = username;
        }

        public Location GetLocation()
        {
            return Location.Offline; // TODO : Set an actual Location
        }

        public string GetLocationString()
        {
            return ""; // TODO : Set an actual Location string
        }

        public Product.ProductCode GetProductCode()
        {
            return Product.ProductCode.None; // TODO : Set an actual ProductCode
        }

        public Status GetStatus()
        {
            return Status.None; // TODO : Set an actual Status
        }
    }
}
