using Atlasd.Battlenet.Sockets;
using System;
using System.Collections.Generic;
using System.Net;

namespace Atlasd.Battlenet.Protocols.Game
{
    class State
    {
        public enum LogonTypes : UInt32
        {
            OLS = 0,
            NLSBeta = 1,
            NLS = 2,
        };

        public ClientState Client { get; protected set; }

        public Account ActiveAccount;
        public Channel ActiveChannel;
        public DateTime ConnectedTimestamp;
        public List<GameKey> GameKeys;
        public IPAddress LocalIPAddress;
        public LocaleInfo Locale;
        public LogonTypes LogonType;
        public DateTime PingDelta;
        public Platform.PlatformCode Platform;
        public Product.ProductCode Product;
        public VersionInfo Version;

        public UInt32 ClientToken;
        public string KeyOwner;
        public string OnlineName;
        public Int32 Ping;
        public UInt32 ProtocolId;
        public UInt32 ServerToken;
        public bool SpawnKey;
        public byte[] Statstring;
        public Int32 TimezoneBias;
        public UInt32 UDPToken;
        public string Username;

        public State(ClientState client)
        {
            var r = new Random();

            Client = client;

            ActiveAccount = null;
            ActiveChannel = null;
            ConnectedTimestamp = DateTime.Now;
            GameKeys = new List<GameKey>();
            LocalIPAddress = null;
            Locale = new LocaleInfo();
            LogonType = LogonTypes.OLS;
            PingDelta = DateTime.Now;
            Platform = Battlenet.Platform.PlatformCode.None;
            Product = Battlenet.Product.ProductCode.None;
            Version = new VersionInfo();

            ClientToken = 0;
            KeyOwner = null;
            OnlineName = null;
            Ping = -1;
            ProtocolId = 0;
            ServerToken = (uint)r.Next(0, 0x7FFFFFFF);
            SpawnKey = false;
            Statstring = new byte[4];
            TimezoneBias = 0;
            UDPToken = (uint)r.Next(0, 0x7FFFFFFF);
            Username = null;
        }
    }
}
