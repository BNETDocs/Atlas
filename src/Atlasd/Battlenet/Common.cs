using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Battlenet.Protocols.Udp;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Atlasd.Battlenet
{
    class Common
    {
        public const string NewLine = "\r\n"; // This should be the same line ending format of the Atlasd/Localization/Resources.resx XML document.

        public struct ShutdownEvent
        {
            public string AdminMessage { get; private set; }
            public bool Cancelled { get; private set; }
            public DateTime EventDate { get; private set; }
            public Timer EventTimer { get; private set; }

            public ShutdownEvent(string adminMessage, bool cancelled, DateTime eventDate, Timer eventTimer)
            {
                AdminMessage = adminMessage;
                Cancelled = cancelled;
                EventDate = eventDate;
                EventTimer = eventTimer;
            }
        };

        public static ConcurrentDictionary<string, Account> AccountsDb;
        public static ConcurrentDictionary<string, Account> AccountsProcessing;
        public static ConcurrentDictionary<string, Account> ActiveAccounts;
        public static List<Advertisement> ActiveAds;
        public static Dictionary<string, Channel> ActiveChannels;
        public static List<ClientState> ActiveClientStates;
        public static ConcurrentDictionary<byte[], GameAd> ActiveGameAds;
        public static ConcurrentDictionary<string, GameState> ActiveGameStates;
        public static IPAddress DefaultAddress { get; private set; }
        public static int DefaultPort { get; private set; }
        public static ServerSocket Listener { get; private set; }
        public static IPAddress ListenerAddress { get; private set; }
        public static IPEndPoint ListenerEndPoint { get; private set; }
        public static int ListenerPort { get; private set; }
        public static Timer NullTimer { get; private set; }
        public static Timer PingTimer { get; private set; }
        public static ShutdownEvent ScheduledShutdown { get; private set; }
        public static UdpListener UdpListener { get; private set; }

        public static string GetServerGreeting(ClientState receiver)
        {
            var r = Resources.ChannelFirstJoinGreeting;

            r = r.Replace("{host}", "BNETDocs");
            r = r.Replace("{serverStats}", GetServerStats(receiver));
            r = r.Replace("{realm}", "Battle.net");

            return r;
        }

        public static string GetServerStats(ClientState receiver)
        {
            if (receiver == null || receiver.GameState == null || receiver.GameState.ActiveChannel == null) return "";

            var channel = receiver.GameState.ActiveChannel;
            var numGameOnline = GetActiveClientCountByProduct(receiver.GameState.Product);
            var numGameAdvertisements = 0;
            var numTotalOnline = ActiveClientStates.Count;
            var numTotalAdvertisements = 0;
            var strGame = Product.ProductName(receiver.GameState.Product, true);

            var r = Resources.ServerStatistics;

            r = r.Replace("{channel}", channel.Name);
            r = r.Replace("{host}", "BNETDocs");
            r = r.Replace("{game}", strGame);
            r = r.Replace("{gameUsers}", numGameOnline.ToString("#,0"));
            r = r.Replace("{gameAds}", numGameAdvertisements.ToString("#,0"));
            r = r.Replace("{realm}", "Battle.net");
            r = r.Replace("{totalUsers}", numTotalOnline.ToString("#,0"));
            r = r.Replace("{totalGameAds}", numTotalAdvertisements.ToString("#,0"));

            return r;
        }

        public static void Initialize()
        {
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Initializing Battle.net common state");

            AccountsDb = new ConcurrentDictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            AccountsProcessing = new ConcurrentDictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            ActiveAccounts = new ConcurrentDictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            ActiveChannels = new Dictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);
            ActiveClientStates = new List<ClientState>();
            ActiveGameAds = new ConcurrentDictionary<byte[], GameAd>();
            ActiveGameStates = new ConcurrentDictionary<string, GameState>(StringComparer.OrdinalIgnoreCase);

            InitializeAds();

            DefaultAddress = IPAddress.Any;
            DefaultPort = 6112;
            InitializeListener();

            NullTimer = new Timer(ProcessNullTimer, ActiveGameStates, 100, 100);
            PingTimer = new Timer(ProcessPingTimer, ActiveGameStates, 100, 100);

            ScheduledShutdown = new ShutdownEvent(null, true, DateTime.MinValue, null);

            Daemon.Common.TcpNoDelay = Settings.GetBoolean(new string[] { "battlenet", "listener", "tcp_nodelay" }, true);
        }

        public static void InitializeAds()
        {
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Initializing advertisements");

            if (ActiveAds == null || ActiveAds.Count != 0)
            {
                ActiveAds = new List<Advertisement>();
            }

            Settings.State.RootElement.TryGetProperty("ads", out var adsJson);

            foreach (var adJson in adsJson.EnumerateArray())
            {
                adJson.TryGetProperty("enabled", out var enabledJson);
                adJson.TryGetProperty("filename", out var filenameJson);
                adJson.TryGetProperty("url", out var urlJson);
                adJson.TryGetProperty("product", out var productsJson);
                adJson.TryGetProperty("locale", out var localesJson);

                var enabled = enabledJson.GetBoolean();
                var filename = filenameJson.GetString();
                var url = urlJson.GetString();

                List<Product.ProductCode> products = null;
                List<uint> locales = null;

                if (productsJson.ValueKind == JsonValueKind.Array)
                {
                    foreach (var productJson in productsJson.EnumerateArray())
                    {
                        var productStr = productJson.GetString();
                    }
                }

                if (localesJson.ValueKind == JsonValueKind.Array)
                {
                    foreach (var localeJson in localesJson.EnumerateArray())
                    {
                        var localeId = localeJson.GetUInt32();
                    }
                }

                var ad = new Advertisement(filename, url, products, locales);

                lock (ActiveAds) ActiveAds.Add(ad);
            }

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Config, $"Initialized {ActiveAds.Count} advertisements");
        }

        private static void InitializeListener()
        {
            Settings.State.RootElement.TryGetProperty("battlenet", out var battlenetJson);
            battlenetJson.TryGetProperty("listener", out var listenerJson);
            listenerJson.TryGetProperty("interface", out var interfaceJson);
            listenerJson.TryGetProperty("port", out var portJson);

            var listenerAddressStr = interfaceJson.GetString();
            if (!IPAddress.TryParse(listenerAddressStr, out IPAddress listenerAddress))
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Unable to parse IP address from [battlenet.listener.interface] with value [{listenerAddressStr}]; using any");
                listenerAddress = DefaultAddress;
            }
            ListenerAddress = listenerAddress;

            portJson.TryGetInt32(out var port);
            ListenerPort = port;

            if (!IPEndPoint.TryParse($"{ListenerAddress}:{ListenerPort}", out IPEndPoint listenerEndPoint))
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Unable to parse endpoint with value [{ListenerAddress}:{ListenerPort}]");
                return;
            }
            ListenerEndPoint = listenerEndPoint;

            UdpListener = new UdpListener(ListenerEndPoint);
            Listener = new ServerSocket(ListenerEndPoint);
        }
        
        public static uint GetActiveClientCountByProduct(Product.ProductCode productCode)
        {
            var count = (uint)0;

            lock (ActiveClientStates)
            {
                foreach (var client in ActiveClientStates)
                {
                    if (client == null || client.GameState == null || client.GameState.Product == Product.ProductCode.None) continue;
                    if (client.GameState.Product == productCode) count++;
                }
            }

            return count;
        }

        public static bool GetClientByOnlineName(string target, out GameState client)
        {
            var t = target;

            // Escape out of Diablo II character name designation
            if (t.Contains('*'))
            {
                t = t[(t.IndexOf('*') + 1)..];
            }

            // Escape out of gateway designation
            if (t.Contains('#'))
            {
                var n = t[(t.LastIndexOf('#') + 1)..];
                if (!int.TryParse(n, out var i) || i == 0)
                {
                    t = t[0..t.LastIndexOf('#')];
                }
            }

            return ActiveGameStates.TryGetValue(t, out client);
        }

        static void ProcessNullTimer(object state)
        {
            var gameStates = state as ConcurrentDictionary<string, GameState>;
            var msg = new SID_NULL();
            var interval = TimeSpan.FromSeconds(60);
            var now = DateTime.Now;

            foreach (var gameState in gameStates.Values)
            {
                if (gameState == null) continue;
                if (gameState.Client == null) continue;
                if (!gameState.Client.Connected) continue;
                if (gameState.LastNull == null || gameState.LastNull + interval > now) continue;

                gameState.LastNull = now;
                msg.Invoke(new MessageContext(gameState.Client, Protocols.MessageDirection.ServerToClient));
                gameState.Client.Send(msg.ToByteArray(gameState.Client.ProtocolType));
            }
        }

        static void ProcessPingTimer(object state)
        {
            var gameStates = state as ConcurrentDictionary<string, GameState>;
            var msg = new SID_PING();
            var r = new Random();
            var interval = TimeSpan.FromSeconds(180);
            var unresponsiveInterval = interval.Multiply(2);
            var unresponsiveIntervalMS = (int)Math.Round(unresponsiveInterval.TotalMilliseconds);
            var now = DateTime.Now;

            foreach (var gameState in gameStates.Values)
            {
                if (gameState == null) continue;
                if (gameState.Client == null) continue;
                if (!gameState.Client.Connected) continue;
                if (gameState.LastPing == null) continue;

                if (gameState.LastPing + interval <= now)
                {
                    gameState.LastPing = now;
                    gameState.PingToken = (uint)r.Next(0, 0x7FFFFFFF);

                    msg.Invoke(new MessageContext(gameState.Client, Protocols.MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){{ "token", gameState.PingToken }}));
                    gameState.Client.Send(msg.ToByteArray(gameState.Client.ProtocolType));
                }

                if (gameState.LastPong + unresponsiveInterval <= now && gameState.ActiveChannel != null && gameState.Ping != unresponsiveIntervalMS)
                {
                    gameState.ActiveChannel.UpdateUser(gameState, unresponsiveIntervalMS);
                }
            }
        }

        public static void ScheduleShutdown(TimeSpan period, string message = null, ChatCommandContext command = null)
        {
            var rescheduled = false;
            if (ScheduledShutdown.EventTimer != null)
            {
                rescheduled = true;
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, "Stopping previously scheduled shutdown timer");
                ScheduledShutdown.EventTimer.Dispose();
            }

            ScheduledShutdown = new ShutdownEvent(message, false, DateTime.Now + period,
                new Timer((object state) =>
                {
                    Program.ExitCode = 0;
                    Program.Exit = true;
                }, command, period, period)
            );

            var tsStr = $"{period.Hours} hour{(period.Hours == 1 ? "" : "s")} {period.Minutes} minute{(period.Minutes == 1 ? "" : "s")} {period.Seconds} second{(period.Seconds == 1 ? "" : "s")}";

            tsStr = tsStr.Replace("0 hours ", "");
            tsStr = tsStr.Replace("0 minutes ", "");
            tsStr = tsStr.Replace(" 0 seconds", "");

            string m;
            if (string.IsNullOrEmpty(message) && !rescheduled)
            {
                m = Resources.ServerShutdownScheduled;
            }
            else if (string.IsNullOrEmpty(message) && rescheduled)
            {
                m = Resources.ServerShutdownRescheduled;
            }
            else if (!string.IsNullOrEmpty(message) && !rescheduled)
            {
                m = Resources.ServerShutdownScheduledWithMessage;
            }
            else if (!string.IsNullOrEmpty(message) && rescheduled)
            {
                m = Resources.ServerShutdownRescheduledWithMessage;
            }
            else
            {
                throw new InvalidOperationException("Cannot set server shutdown message from localized resource");
            }
            m = m.Replace("{period}", tsStr);
            m = m.Replace("{message}", message);

            Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, m);

            Task.Run(() =>
            {
                var chatEvent = new ChatEvent(ChatEvent.EventIds.EID_BROADCAST, Account.Flags.Admin, -1, "Battle.net", m);
                foreach (var gameState in ActiveGameStates.Values) chatEvent.WriteTo(gameState.Client);

                if (command != null)
                {
                    var r = Resources.AdminShutdownCommandScheduled;

                    foreach (var kv in command.Environment)
                    {
                        r = r.Replace("{" + kv.Key + "}", kv.Value);
                    }

                    foreach (var line in r.Split(Environment.NewLine))
                        new ChatEvent(ChatEvent.EventIds.EID_INFO, (uint)command.GameState.ChannelFlags, command.GameState.Ping, command.GameState.OnlineName, line).WriteTo(command.GameState.Client);
                }
            });
        }

        public static void ScheduleShutdownCancelled(string message = null, ChatCommandContext command = null)
        {
            if (ScheduledShutdown.Cancelled)
            {
                if (command != null)
                {
                    var r = Resources.AdminShutdownCommandNotScheduled;

                    foreach (var kv in command.Environment)
                    {
                        r = r.Replace("{" + kv.Key + "}", kv.Value);
                    }

                    foreach (var line in r.Split(Environment.NewLine))
                        new ChatEvent(ChatEvent.EventIds.EID_ERROR, (uint)command.GameState.ChannelFlags, command.GameState.Ping, command.GameState.OnlineName, line).WriteTo(command.GameState.Client);
                }
            }
            else
            {
                var m = string.IsNullOrEmpty(message) ? Resources.ServerShutdownCancelled : Resources.ServerShutdownCancelledWithMessage;
                m = m.Replace("{message}", message);

                if (ScheduledShutdown.EventTimer != null)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, "Stopping previously scheduled shutdown event");
                    ScheduledShutdown.EventTimer.Dispose();
                }

                ScheduledShutdown = new ShutdownEvent(
                    ScheduledShutdown.AdminMessage,
                    true, // Cancelled
                    ScheduledShutdown.EventDate,
                    ScheduledShutdown.EventTimer
                );

                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, m);

                Task.Run(() =>
                {
                    var chatEvent = new ChatEvent(ChatEvent.EventIds.EID_BROADCAST, Account.Flags.Admin, -1, "Battle.net", m);
                    foreach (var gameState in ActiveGameStates.Values) chatEvent.WriteTo(gameState.Client);

                    if (command != null)
                    {
                        var r = Resources.AdminShutdownCommandCancelled;

                        foreach (var kv in command.Environment)
                        {
                            r = r.Replace("{" + kv.Key + "}", kv.Value);
                        }

                        foreach (var line in r.Split(Environment.NewLine))
                            new ChatEvent(ChatEvent.EventIds.EID_INFO, (uint)command.GameState.ChannelFlags, command.GameState.Ping, command.GameState.OnlineName, line).WriteTo(command.GameState.Client);
                    }
                });
            }
        }
    }
}
