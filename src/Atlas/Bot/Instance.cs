using Atlas.Battlenet;

using System;

namespace Atlas.Bot
{
    class Instance
    {
        public string Name;

        public string Username;
        public string Password;

        public string BattlenetHost;
        public UInt16 BattlenetPort;

        public string BNLSHost;
        public UInt16 BNLSPort;

        public Platform.PlatformCode Platform;
        public Product.ProductCode Product;
        public UInt32 VersionByte;

        public string GameKey1;
        public string GameKey2;
        public string GameKeyOwner;

        public string EmailAddress;

        public string HomeChannel;

        public string ProxyHost;
        public UInt16 ProxyPort;
        public Battlenet.Sockets.Proxy.ProxyType ProxyType;
        public string ProxyUsername;
        public string ProxyPassword;

        public bool AutoReconnect;
        public bool PingSpoof;
        public bool PingUDP;

        public Instance(bool prompt)
        {
            if (prompt)
                Prompt();
        }

        public void Prompt()
        {
            int i;

            Console.Write("[Instance] Instance Name: ");
            Name = Console.ReadLine();

            Console.Write("[Instance] Username: ");
            Username = Console.ReadLine();

            Console.Write("[Instance] Password: ");
            Password = Console.ReadLine();

            Console.Write("[Instance] Battle.net Host: ");
            BattlenetHost = Console.ReadLine();

            i = BattlenetHost.IndexOf(":");
            if (i > 0)
            {
                UInt16.TryParse(BattlenetHost.Substring(i + 1, BattlenetHost.Length - i - 1), out BattlenetPort);
                BattlenetHost = BattlenetHost.Substring(0, i);
            }

            if (BattlenetPort == 0)
            {
                Console.Write("[Instance] Battle.net Port (default 6112): ");
                UInt16.TryParse(Console.ReadLine(), out BattlenetPort);
            }

            if (BattlenetPort == 0)
            {
                Console.WriteLine("[Instance] Defaulting Battle.net port number to 6112.");
                BattlenetPort = 6112;
            }

            Console.Write("[Instance] BNLS Host: ");
            BNLSHost = Console.ReadLine();

            i = BNLSHost.IndexOf(":");
            if (i > 0)
            {
                UInt16.TryParse(BNLSHost.Substring(i + 1, BNLSHost.Length - i - 1), out BNLSPort);
                BNLSHost = BNLSHost.Substring(0, i);
            }

            if (BNLSPort == 0)
            {
                Console.Write("[Instance] BNLS Port (default 9367): ");
                UInt16.TryParse(Console.ReadLine(), out BNLSPort);
            }

            if (BNLSPort == 0)
            {
                Console.WriteLine("[Instance] Defaulting BNLS port number to 9367.");
                BNLSPort = 9367;
            }

            Platform = Battlenet.Platform.PlatformCode.Windows;

            Console.WriteLine("[Instance] Choose the number for the product:");
            Console.WriteLine("[Instance]    1 - Chat");
            Console.WriteLine("[Instance]    2 - Diablo");
            Console.WriteLine("[Instance]    3 - Diablo Shareware");
            Console.WriteLine("[Instance]    4 - Diablo II");
            Console.WriteLine("[Instance]    5 - Diablo II Lord of Destruction");
            Console.WriteLine("[Instance]    6 - Starcraft Original");
            Console.WriteLine("[Instance]    7 - Starcraft Broodwar");
            Console.WriteLine("[Instance]    8 - Starcraft Japanese");
            Console.WriteLine("[Instance]    9 - Starcraft Shareware");
            Console.WriteLine("[Instance]   10 - Warcraft II");
            Console.WriteLine("[Instance]   11 - Warcraft III Demo");
            Console.WriteLine("[Instance]   12 - Warcraft III Reign of Chaos");
            Console.WriteLine("[Instance]   13 - Warcraft III The Frozen Throne");
            i = 0;
            while (i < 1 || i > 13)
            {
                Console.Write("[Instance]   Choice: ");
                int.TryParse(Console.ReadLine(), out i);
            }
            switch (i) {
                case  1: Product = Battlenet.Product.ProductCode.Chat; break;
                case  2: Product = Battlenet.Product.ProductCode.DiabloRetail; break;
                case  3: Product = Battlenet.Product.ProductCode.DiabloShareware; break;
                case  4: Product = Battlenet.Product.ProductCode.DiabloII; break;
                case  5: Product = Battlenet.Product.ProductCode.DiabloIILordOfDestruction; break;
                case  6: Product = Battlenet.Product.ProductCode.StarcraftOriginal; break;
                case  7: Product = Battlenet.Product.ProductCode.StarcraftBroodwar; break;
                case  8: Product = Battlenet.Product.ProductCode.StarcraftJapanese; break;
                case  9: Product = Battlenet.Product.ProductCode.StarcraftShareware; break;
                case 10: Product = Battlenet.Product.ProductCode.WarcraftII; break;
                case 11: Product = Battlenet.Product.ProductCode.WarcraftIIIDemo; break;
                case 12: Product = Battlenet.Product.ProductCode.WarcraftIIIReignOfChaos; break;
                case 13: Product = Battlenet.Product.ProductCode.WarcraftIIIFrozenThrone; break;
            }

            Console.Write("[Instance] Version Byte: ");
            UInt32.TryParse(Console.ReadLine(), out VersionByte);

            Console.Write("[Instance] Game Key 1: ");
            GameKey1 = Console.ReadLine();

            Console.Write("[Instance] Game Key 2: ");
            GameKey2 = Console.ReadLine();

            Console.Write("[Instance] Game Key Owner: ");
            GameKeyOwner = Console.ReadLine();

            Console.Write("[Instance] Email Address: ");
            EmailAddress = Console.ReadLine();

            Console.Write("[Instance] Home Channel: ");
            HomeChannel = Console.ReadLine();

            Console.WriteLine("[Instance] Choose the number for the proxy type:");
            Console.WriteLine("[Instance]    0 - Disabled");
            Console.WriteLine("[Instance]    1 - Socks 4");
            Console.WriteLine("[Instance]    2 - Socks 5");
            Console.WriteLine("[Instance]    3 - HTTP");
            i = 0;
            while (i < 0 || i > 3)
            {
                Console.Write("[Instance]   Choice: ");
                int.TryParse(Console.ReadLine(), out i);
            }
            switch (i)
            {
                case 0: ProxyType = Battlenet.Sockets.Proxy.ProxyType.Disabled; break;
                case 1: ProxyType = Battlenet.Sockets.Proxy.ProxyType.Socks4; break;
                case 2: ProxyType = Battlenet.Sockets.Proxy.ProxyType.Socks5; break;
                case 3: ProxyType = Battlenet.Sockets.Proxy.ProxyType.HTTP; break;
            }

            if (ProxyType != Battlenet.Sockets.Proxy.ProxyType.Disabled)
            {
                Console.Write("Proxy Host: ");
                ProxyHost = Console.ReadLine();

                i = ProxyHost.IndexOf(":");
                if (i > 0)
                {
                    UInt16.TryParse(ProxyHost.Substring(i + 1, ProxyHost.Length - i - 1), out ProxyPort);
                    ProxyHost = ProxyHost.Substring(0, i);
                }

                if (ProxyPort == 0)
                {
                    Console.Write("[Instance] Proxy Port (default 1080): ");
                    UInt16.TryParse(Console.ReadLine(), out ProxyPort);
                }

                if (ProxyPort == 0)
                {
                    Console.WriteLine("[Instance] Defaulting proxy port number to 1080.");
                    ProxyPort = 1080;
                }

                Console.Write("[Instance] Proxy Username: ");
                ProxyUsername = Console.ReadLine();

                Console.Write("[Instance] Proxy Password: ");
                ProxyPassword = Console.ReadLine();
            }

            AutoReconnect = true;
            PingSpoof = false;
            PingUDP = true;
        }

        public void Deserialize(BinaryReader buffer)
        {
            Name = buffer.ReadString();
            Username = buffer.ReadString();
            Password = buffer.ReadString();
            BattlenetHost = buffer.ReadString();
            BattlenetPort = buffer.ReadUInt16();
            BNLSHost = buffer.ReadString();
            BNLSPort = buffer.ReadUInt16();
            Platform = (Platform.PlatformCode)buffer.ReadUInt32();
            Product = (Product.ProductCode)buffer.ReadUInt32();
            VersionByte = buffer.ReadUInt32();
            GameKey1 = buffer.ReadString();
            GameKey2 = buffer.ReadString();
            GameKeyOwner = buffer.ReadString();
            EmailAddress = buffer.ReadString();
            HomeChannel = buffer.ReadString();
            ProxyType = (Battlenet.Sockets.Proxy.ProxyType)buffer.ReadByte();
            ProxyHost = buffer.ReadString();
            ProxyPort = buffer.ReadUInt16();
            ProxyUsername = buffer.ReadString();
            ProxyPassword = buffer.ReadString();
            AutoReconnect = buffer.ReadBoolean();
            PingSpoof = buffer.ReadBoolean();
            PingUDP = buffer.ReadBoolean();
        }

        public void Serialize(BinaryWriter buffer)
        {
            buffer.Write(Name);
            buffer.Write(Username);
            buffer.Write(Password);
            buffer.Write(BattlenetHost);
            buffer.Write(BattlenetPort);
            buffer.Write(BNLSHost);
            buffer.Write(BNLSPort);
            buffer.Write((UInt32)Platform);
            buffer.Write((UInt32)Product);
            buffer.Write(VersionByte);
            buffer.Write(GameKey1);
            buffer.Write(GameKey2);
            buffer.Write(GameKeyOwner);
            buffer.Write(EmailAddress);
            buffer.Write(HomeChannel);
            buffer.Write((byte)ProxyType);
            buffer.Write(ProxyHost);
            buffer.Write(ProxyPort);
            buffer.Write(ProxyUsername);
            buffer.Write(ProxyPassword);
            buffer.Write(AutoReconnect);
            buffer.Write(PingSpoof);
            buffer.Write(PingUDP);
        }
    }
}
