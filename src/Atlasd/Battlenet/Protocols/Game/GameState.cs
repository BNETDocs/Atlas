using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
        public Clan ActiveClan;
        public Account.Flags ChannelFlags;
        public DateTime ConnectedTimestamp;
        public GameAd GameAd;
        public IPAddress GameDataAddress;
        public List<GameKey> GameKeys;
        public DateTime LastLogon;
        public DateTime LastNull;
        public DateTime LastPing;
        public DateTime LastPong;
        public IPAddress LocalIPAddress;
        public DateTime LocalTime { get => DateTime.UtcNow.AddMinutes(0 - TimezoneBias); }
        public LocaleInfo Locale;
        public LogonTypes LogonType;
        public List<IPAddress> SquelchedIPs;
        public Platform.PlatformCode Platform;
        public Product.ProductCode Product;
        public VersionInfo Version;

        public string Away;
        public byte[] CharacterName;
        public UInt32 ClientToken;
        public string DoNotDisturb;
        public UInt32 FailedLogons;
        public ushort GameDataPort;
        public byte[] KeyOwner;
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
            ActiveClan = null;
            ChannelFlags = Account.Flags.None;
            ConnectedTimestamp = DateTime.Now;
            GameAd = null;
            GameDataAddress = null;
            GameKeys = new List<GameKey>();
            LastLogon = DateTime.Now;
            LastNull = DateTime.Now;
            LastPing = DateTime.MinValue;
            LastPong = DateTime.Now;
            LocalIPAddress = null;
            Locale = new LocaleInfo();
            LogonType = LogonTypes.OLS;
            SquelchedIPs = new List<IPAddress>();
            Platform = Battlenet.Platform.PlatformCode.None;
            Product = Battlenet.Product.ProductCode.None;
            Version = new VersionInfo();

            Away = null;
            DoNotDisturb = null;
            CharacterName = new byte[0];
            ClientToken = 0;
            FailedLogons = 0;
            GameDataPort = 0;
            KeyOwner = null;
            OnlineName = null;
            Ping = -1;
            PingToken = (uint)r.Next(0, 0x7FFFFFFF);
            ProtocolId = 0;
            ServerToken = (uint)r.Next(0, 0x7FFFFFFF);
            Statstring = null;
            SpawnKey = false;
            TimezoneBias = 0;
            UDPSupported = false;
            UDPToken = (uint)r.Next(0, 0x7FFFFFFF);
            Username = null;
        }

        public static bool CanStatstringUpdate(Product.ProductCode code)
        {
            bool allow = false;
            string codeStr = Battlenet.Product.ToString(code);
            string codeStrR = new string(codeStr.Reverse().ToArray());
            if (!allow) allow = Settings.GetBoolean(new string[]{ "battlenet", "emulation", "statstring_updates", codeStr }, false, true); // Ltrd
            if (!allow) allow = Settings.GetBoolean(new string[]{ "battlenet", "emulation", "statstring_updates", codeStrR }, false, true); // Drtl
            if (!allow) allow = Settings.GetBoolean(new string[]{ "battlenet", "emulation", "statstring_updates", codeStr.ToUpperInvariant() }, false, true); // LTRD
            if (!allow) allow = Settings.GetBoolean(new string[]{ "battlenet", "emulation", "statstring_updates", codeStrR.ToUpperInvariant() }, false, true); // DRTL
            if (!allow) allow = Settings.GetBoolean(new string[]{ "battlenet", "emulation", "statstring_updates", codeStr.ToLowerInvariant() }, false, true); // ltrd
            if (!allow) allow = Settings.GetBoolean(new string[]{ "battlenet", "emulation", "statstring_updates", codeStrR.ToLowerInvariant() }, false, true); // drtl
            return allow;
        }

        public void Close()
        {
            // Remove this GameState from ActiveChannel
            if (ActiveChannel != null)
            {
                ActiveChannel.RemoveUser(this); // will change this.ActiveChannel to null.
            }

            // Notify clan members
            if (ActiveClan != null)
            {
                ActiveClan.WriteStatusChange(this, false); // offline
            }

            // Update keys of ActiveAccount
            if (ActiveAccount != null)
            {
                ActiveAccount.Set(Account.LastLogoffKey, DateTime.Now);

                var timeLogged = (UInt32)ActiveAccount.Get(Account.TimeLoggedKey);
                var diff = DateTime.Now - ConnectedTimestamp;
                timeLogged += (UInt32)Math.Round(diff.TotalSeconds);
                ActiveAccount.Set(Account.TimeLoggedKey, timeLogged);
            }

            // Remove this OnlineName from ActiveAccounts and ActiveGameStates
            if (!string.IsNullOrEmpty(OnlineName))
            {
                Battlenet.Common.ActiveAccounts.TryRemove(OnlineName, out _);
                Battlenet.Common.ActiveGameStates.TryRemove(OnlineName, out _);
            }

            // Remove this GameAd
            if (GameAd != null && GameAd.RemoveClient(this)) GameAd = null;
        }

        public void Dispose() /* part of IDisposable */
        {
            if (IsDisposing) return;
            IsDisposing = true;

            Close(); // call our public cleanup method

            IsDisposing = false;
        }

        public void GenerateLocaleFromGeoIP()
        {
            var file = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "GeoIP2-City.mmdb"));
            // This creates the DatabaseReader object, which should be reused across
            // lookups.
            using (var reader = new MaxMind.GeoIP2.DatabaseReader(file))
            {
                // Replace "City" with the appropriate method for your database, e.g.,
                // "Country".
                var city = reader.City((Client.RemoteEndPoint as IPEndPoint).Address.ToString());

                Console.WriteLine(city.Country.IsoCode); // 'US'
                Console.WriteLine(city.Country.Name); // 'United States'
                Console.WriteLine(city.Country.Names["zh-CN"]); // '美国'

                Console.WriteLine(city.MostSpecificSubdivision.Name); // 'Minnesota'
                Console.WriteLine(city.MostSpecificSubdivision.IsoCode); // 'MN'

                Console.WriteLine(city.City.Name); // 'Minneapolis'

                Console.WriteLine(city.Postal.Code); // '55455'

                Console.WriteLine(city.Location.Latitude); // 44.9733
                Console.WriteLine(city.Location.Longitude); // -93.2323
            }
        }

        public byte[] GenerateStatstring()
        {
            byte[] statstring = null;
            byte[] buf = null;
            MemoryStream m = null;
            BinaryWriter w = null;

            try
            {
                buf = new byte[128];
                m = new MemoryStream(buf);
                w = new BinaryWriter(m);

                w.Write((UInt32)Product);
                var product = new byte[4];
                Buffer.BlockCopy(buf, 0, product, 0, product.Length);
                var game = Encoding.UTF8.GetString(product);

                // Product is not SSHR, but is STAR/SEXP/JSTR or W2BN:
                if (Product != Battlenet.Product.ProductCode.StarcraftShareware && (
                    Battlenet.Product.IsStarcraft(Product) || Battlenet.Product.IsWarcraftII(Product)))
                {
                    /**
                     * Contain 9 fields that are delimited with spaces.
                     * Ladder Rating
                     * Ladder Rank
                     * Wins
                     *     The amount of wins in normal games
                     * Spawned
                     *     '0': Not spawned
                     *     '1': Spawned
                     * League ID
                     * High Ladder Rating
                     *     The highest rating that the player has ever achieved
                     * IronMan Ladder Rating
                     *     Only applicable to: WarCraft II
                     * IronMan Ladder Rank
                     *     Only applicable to: WarCraft II
                     * Icon Code
                     *     Only applicable to: StarCraft, StarCraft Japanese, StarCraft: Brood War
                     *     This value should be searched for in the Icon Code array of each icon in each Battle.net Icon file that is loaded. If a match is found, the client should use this icon when displaying the user. See Icons.bni.
                     */

                    var ladderRating = (uint)0;
                    var ladderRank = (uint)0;
                    var wins = (uint)ActiveAccount.Get($"record\\{game}\\0\\wins", 0);
                    var leagueId = (uint)ActiveAccount.Get("System\\League", 0);
                    var highLadderRating = (uint)ActiveAccount.Get($"record\\{game}\\1\\rating", 0);
                    var ironManLadderRating = (uint)ActiveAccount.Get($"record\\{game}\\3\\rating", 0);
                    var ironManLadderRank = (uint)ActiveAccount.Get($"record\\{game}\\3\\rank", 0);
                    var iconCode = (byte[])ActiveAccount.Get("System\\Icon", product);

                    w.Write(' ');
                    w.Write(Encoding.UTF8.GetBytes(ladderRating.ToString()));
                    w.Write(' ');
                    w.Write(Encoding.UTF8.GetBytes(ladderRank.ToString()));
                    w.Write(' ');
                    w.Write(Encoding.UTF8.GetBytes(wins.ToString()));
                    w.Write(' ');
                    w.Write(SpawnKey ? '1' : '0');
                    w.Write(' ');
                    w.Write(Encoding.UTF8.GetBytes(leagueId.ToString()));
                    w.Write(' ');
                    w.Write(Encoding.UTF8.GetBytes(highLadderRating.ToString()));
                    w.Write(' ');
                    w.Write(Encoding.UTF8.GetBytes(ironManLadderRating.ToString()));
                    w.Write(' ');
                    w.Write(Encoding.UTF8.GetBytes(ironManLadderRank.ToString()));
                    w.Write(' ');
                    w.Write(iconCode);
                }

                if (Battlenet.Product.IsWarcraftIII(Product))
                {
                    /**
                     * Contain 2 fields and 1 optional field, all fields are delimited with spaces.
                     * There is a possibility that there can be 0 fields, meaning that the user was not assigned their stats before joining the channel (often appears with bots who join a channel automatically and not waiting until the user clicks 'Enter Chat').
                     *
                     *     Icon Code
                     *         Format: Level + Tier + "3W" (Special icons may not follow this format)
                     *             Level: The "win level" of the icon 1 through 5 (6 on TFT). 1 is always peon.
                     *             Tier: The race tier of the icon
                     *                 R: Random
                     *                 H: Human
                     *                 U: Undead
                     *                 N: Night Elf
                     *                 O: Orc
                     *                 D: Tournament (TFT)
                     *         This value should be searched for in the Icon Code array of each icon in each Battle.net Icon file that is loaded. If a match is found, the client should use this icon when displaying the user. See Icons.bni.
                     *     Level
                     *         Level of the player. (Highest out of all possible game types that the user has played.) '0' means no ladder games on record.
                     *     Clan tag (OPTIONAL)
                     *         Reversed clan tag, appears only if the player is in a clan.
                     */

                    //var iconCode = ActiveAccount.Get("System\\Icon", product);
                    var iconCode = 0x57335231; // W3R1
                    var ladderLevel = (uint)0;
                    var clanTag = (byte[])ActiveAccount.Get("System\\Clan", new byte[] { 0, 0, 0, 0 });

                    w.Write(' ');
                    w.Write(iconCode);
                    w.Write(' ');
                    w.Write(Encoding.UTF8.GetBytes(ladderLevel.ToString()));

                    if (!clanTag.SequenceEqual(new byte[] { 0, 0, 0, 0 }))
                    {
                        w.Write(' ');
                        w.Write(clanTag);
                    }
                }
            }
            finally
            {
                if (w == null)
                {
                    statstring = buf;
                }
                else
                {
                    statstring = new byte[(int)w.BaseStream.Position];
                    Buffer.BlockCopy(buf, 0, statstring, 0, (int)w.BaseStream.Position);
                    w.Close();
                }

                if (m != null) m.Close();
            }

            return statstring;
        }

        /**
         * <remarks>Checks whether a user has administrative power on the server.</remarks>
         * <param name="user">The user to check whether they have admin status.</param>
         * <param name="includeChannelOp">Defaults to false. If user has flags 0x02 ChannelOp and includeChannelOp is true, then they will be considered having admin.</param>
         */
        public static bool HasAdmin(GameState user, bool includeChannelOp = false)
        {
            var grantSudoToSpoofedAdmins = Settings.GetBoolean(new string[] { "battlenet", "emulation", "grant_sudo_to_spoofed_admins" }, false);
            var hasSudo = false;
            lock (user)
            {
                var userFlags = (Account.Flags)user.ActiveAccount.Get(Account.FlagsKey);
                hasSudo =
                    (
                        grantSudoToSpoofedAdmins && (
                            user.ChannelFlags.HasFlag(Account.Flags.Admin) ||
                            (user.ChannelFlags.HasFlag(Account.Flags.ChannelOp) && includeChannelOp) ||
                            user.ChannelFlags.HasFlag(Account.Flags.Employee)
                        )
                    )
                    || userFlags.HasFlag(Account.Flags.Admin)
                    || (userFlags.HasFlag(Account.Flags.ChannelOp) && includeChannelOp)
                    || userFlags.HasFlag(Account.Flags.Employee)
                ;
            }
            return hasSudo;
        }

        /**
         * Aliases GameState.HasAdmin(GameState, bool)
         */
        public bool HasAdmin(bool includeChannelOp = false)
        {
            return HasAdmin(this, includeChannelOp);
        }

        public void SetLocale()
        {
            // TODO : Fix this so it works with the asynchronous stuff.
            return;

            /*
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
            */
        }
    }
}
