using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Atlasd.Battlenet.Protocols.Game
{
    class GameState : IDisposable
    {
        public enum LogonTypes : UInt32
        {
            OLS = 0,
            NLSBeta = 1,
            NLS = 2,
        };

        private bool IsDisposing = false;

        public ClientState Client { get; protected set; }

        public Account ActiveAccount;
        public Channel ActiveChannel;
        public Account.Flags ChannelFlags;
        public DateTime ConnectedTimestamp;
        public List<GameKey> GameKeys;
        public DateTime LastLogon;
        public DateTime LastNull;
        public DateTime LastPing;
        public IPAddress LocalIPAddress;
        public DateTime LocalTime { get => DateTime.UtcNow.AddMinutes(0 - TimezoneBias); }
        public LocaleInfo Locale;
        public LogonTypes LogonType;
        public List<IPAddress> SquelchedIPs;
        public DateTime PingDelta;
        public Platform.PlatformCode Platform;
        public Product.ProductCode Product;
        public VersionInfo Version;

        public string Away;
        public string DoNotDisturb;
        public UInt32 ClientToken;
        public string KeyOwner;
        public string OnlineName;
        public Int32 Ping;
        public UInt32 PingToken;
        public UInt32 ProtocolId;
        public UInt32 ServerToken;
        public bool SpawnKey;
        public byte[] Statstring;
        public Int32 TimezoneBias;
        public bool UDPSupported;
        public UInt32 UDPToken;
        public string Username;

        public GameState(ClientState client)
        {
            var r = new Random();

            Client = client;

            ActiveAccount = null;
            ActiveChannel = null;
            ChannelFlags = Account.Flags.None;
            ConnectedTimestamp = DateTime.Now;
            GameKeys = new List<GameKey>();
            LastLogon = DateTime.Now;
            LastNull = DateTime.Now;
            LastPing = DateTime.Parse("1970-01-01T00:00:00+00:00");
            LocalIPAddress = null;
            Locale = new LocaleInfo();
            LogonType = LogonTypes.OLS;
            SquelchedIPs = new List<IPAddress>();
            PingDelta = LastPing;
            Platform = Battlenet.Platform.PlatformCode.None;
            Product = Battlenet.Product.ProductCode.None;
            Version = new VersionInfo();

            Away = null;
            DoNotDisturb = null;
            ClientToken = 0;
            KeyOwner = null;
            OnlineName = null;
            Ping = -1;
            PingToken = (uint)r.Next(0, 0x7FFFFFFF);
            ProtocolId = 0;
            ServerToken = (uint)r.Next(0, 0x7FFFFFFF);
            SpawnKey = false;
            Statstring = null;
            TimezoneBias = 0;
            UDPSupported = false;
            UDPToken = (uint)r.Next(0, 0x7FFFFFFF);
            Username = null;

            Task.Run(() =>
            {
                lock (Battlenet.Common.NullTimerState) Battlenet.Common.NullTimerState.Add(this);
            });
            Task.Run(() =>
            {
                lock (Battlenet.Common.PingTimerState) Battlenet.Common.PingTimerState.Add(this);
            });
        }

        public void Dispose() /* part of IDisposable */
        {
            if (IsDisposing) return;
            IsDisposing = true;

            if (ActiveAccount != null)
            {
                lock (ActiveAccount)
                {
                    ActiveAccount.Set(Account.LastLogoffKey, DateTime.Now);

                    var timeLogged = (UInt32)ActiveAccount.Get(Account.TimeLoggedKey);
                    var diff = DateTime.Now - ConnectedTimestamp;
                    timeLogged += (UInt32)Math.Round(diff.TotalSeconds);
                    ActiveAccount.Set(Account.TimeLoggedKey, timeLogged);

                    var username = (string)ActiveAccount.Get(Account.UsernameKey);
                    if (Battlenet.Common.ActiveAccounts.ContainsKey(username))
                        Battlenet.Common.ActiveAccounts.Remove(username);
                }
            }

            if (ActiveChannel != null)
            {
                lock (ActiveChannel) ActiveChannel.RemoveUser(this);
            }

            if (OnlineName != null)
            {
                lock (Battlenet.Common.ActiveGameStates)
                {
                    if (Battlenet.Common.ActiveGameStates.ContainsKey(OnlineName))
                    {
                        Battlenet.Common.ActiveGameStates.Remove(OnlineName);
                    }
                }
            }

            lock (Battlenet.Common.NullTimerState) Battlenet.Common.NullTimerState.Remove(this);
            lock (Battlenet.Common.PingTimerState) Battlenet.Common.PingTimerState.Remove(this);

            IsDisposing = false;
        }

        public void SetLocale()
        {
            // UserLocaleId is converted to (int) because CultureInfo uses a signed int for "culture"
            var localeId = (int)Locale.UserLocaleId;

            try
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, Client.RemoteEndPoint, $"Setting client locale to [{localeId}]...");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(localeId);
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentOutOfRangeException || ex is CultureNotFoundException)) throw;

                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, Client.RemoteEndPoint, $"Error setting client locale to [{localeId}], using default");
            }
        }
    }
}
