using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Atlasd.Battlenet
{
    class Common
    {

        public static Dictionary<string, Account> AccountsDb;
        public static List<string> AccountsProcessing;
        public static Dictionary<string, Account> ActiveAccounts;
        public static Dictionary<string, Channel> ActiveChannels;
        public static List<ClientState> ActiveClients;
        public static IPAddress DefaultAddress { get; private set; }
        public static int DefaultPort { get; private set; }
        public static ServerSocket Listener { get; private set; }
        public static IPAddress ListenerAddress { get; private set; }
        public static IPEndPoint ListenerEndPoint { get; private set; }
        public static int ListenerPort { get; private set; }

        public static void Initialize()
        {
            AccountsDb = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            AccountsProcessing = new List<string>();
            ActiveAccounts = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            ActiveChannels = new Dictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);
            ActiveClients = new List<ClientState>();

            // Channel object adds itself to ActiveChannels during instantiation.
            new Channel(Channel.TheVoid, Channel.TheVoidFlags, -1);
            new Channel("Backstage", Channel.Flags.Public | Channel.Flags.Restricted, -1, "Abandon hope, all ye who enter here...");
            new Channel("Open Tech Support", Channel.Flags.Public | Channel.Flags.TechSupport, -1);
            new Channel("Blizzard Tech Support", Channel.Flags.Public | Channel.Flags.TechSupport | Channel.Flags.Moderated, -1);
            new Channel("Town Square", Channel.Flags.Public, 200, "Welcome and enjoy your stay!");

            DefaultAddress = IPAddress.Any;
            DefaultPort = 6112;

            InitializeListener();
        }

        private static void InitializeListener()
        {
            var listenerAddressStr = (string)Daemon.Common.Settings["battlenet.listener.interface"];
            if (!IPAddress.TryParse(listenerAddressStr, out IPAddress listenerAddress))
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Unable to parse IP address from [battlenet.listener.interface] with value [{listenerAddressStr}]; using any");
                listenerAddress = DefaultAddress;
            }

            ListenerAddress = listenerAddress;
            ListenerPort = (int)(Daemon.Common.Settings["battlenet.listener.port"] ?? DefaultPort);

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

            lock (ActiveClients)
            {
                foreach (var client in ActiveClients)
                {
                    if (client == null || client.GameState == null || client.GameState.Product == Product.ProductCode.None) continue;
                    if (client.GameState.Product == productCode) count++;
                }
            }

            return count;
        }

        public static string ProtocolTypeName(ProtocolType protocolType)
        {
            return protocolType switch
            {
                ProtocolType.None => "None",
                ProtocolType.Game => "Game",
                ProtocolType.BNFTP => "BNFTP",
                ProtocolType.Chat => "Chat",
                ProtocolType.Chat_Alt1 => "Chat_Alt1",
                ProtocolType.Chat_Alt2 => "Chat_Alt2",
                ProtocolType.IPC => "IPC",
                _ => $"Unknown (0x{(byte)protocolType:X2})",
            };
        }
    }
}
