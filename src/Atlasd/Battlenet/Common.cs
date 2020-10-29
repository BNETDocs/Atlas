using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Atlasd.Battlenet
{
    class Common
    {
        public static Dictionary<string, Account> AccountsDb;
        public static List<string> AccountsProcessing;
        public static Dictionary<string, Account> ActiveAccounts;
        public static Dictionary<string, Channel> ActiveChannels;
        public static List<ClientState> ActiveClientStates;
        public static Dictionary<string, GameState> ActiveGameStates;
        public static IPAddress DefaultAddress { get; private set; }
        public static int DefaultPort { get; private set; }
        public static ServerSocket Listener { get; private set; }
        public static IPAddress ListenerAddress { get; private set; }
        public static IPEndPoint ListenerEndPoint { get; private set; }
        public static int ListenerPort { get; private set; }
        public static Timer NullTimer { get; private set; }
        public static List<GameState> NullTimerState { get; private set; }
        public static Timer PingTimer { get; private set; }
        public static List<GameState> PingTimerState { get; private set; }

        public static void Initialize()
        {
            AccountsDb = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            AccountsProcessing = new List<string>();
            ActiveAccounts = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            ActiveChannels = new Dictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);
            ActiveClientStates = new List<ClientState>();
            ActiveGameStates = new Dictionary<string, GameState>(StringComparer.OrdinalIgnoreCase);

            DefaultAddress = IPAddress.Any;
            DefaultPort = 6112;

            InitializeListener();

            NullTimerState = new List<GameState>();
            PingTimerState = new List<GameState>();

            NullTimer = new Timer(ProcessNullTimer, NullTimerState, 100, 100);
            PingTimer = new Timer(ProcessPingTimer, PingTimerState, 100, 100);
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
            lock (ActiveGameStates)
            {
                return ActiveGameStates.TryGetValue(target, out client);
            }
        }

        static void ProcessNullTimer(object state)
        {
            var clients = state as List<GameState>;
            var msg = new SID_NULL();
            var interval = TimeSpan.FromSeconds(60);
            var now = DateTime.Now;

            lock (clients)
            {
                foreach (var client in clients)
                {
                    if (client == null)
                    {
                        clients.Remove(client);
                        continue;
                    }

                    lock (client)
                    {
                        if (client.LastNull == null || client.LastNull + interval > now) continue;

                        client.LastNull = now;
                        msg.Invoke(new MessageContext(client.Client, Protocols.MessageDirection.ServerToClient));
                        client.Client.Send(msg.ToByteArray());
                    }
                }
            }
        }

        static void ProcessPingTimer(object state)
        {
            var clients = state as List<GameState>;
            var msg = new SID_PING();
            var interval = TimeSpan.FromSeconds(180);
            var now = DateTime.Now;
            var r = new Random();

            lock (clients)
            {
                foreach (var client in clients)
                {
                    if (client == null)
                    {
                        clients.Remove(client);
                        continue;
                    }

                    lock (client)
                    {
                        if (client.LastPing == null || client.LastPing + interval > now) continue;

                        now = DateTime.Now;
                        client.LastPing = now;
                        client.PingDelta = now;
                        client.PingToken = (uint)r.Next(0, 0x7FFFFFFF);

                        msg.Invoke(new MessageContext(client.Client, Protocols.MessageDirection.ServerToClient, new Dictionary<string, object>(){{ "token", client.PingToken }}));
                        client.Client.Send(msg.ToByteArray());
                        client.Client.Socket.Poll(0, System.Net.Sockets.SelectMode.SelectWrite);
                    }
                }
            }
        }
    }
}
