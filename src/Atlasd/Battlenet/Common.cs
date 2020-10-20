using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Atlasd.Battlenet
{
    class Common
    {

        public static Dictionary<string, Account> AccountsDb;
        public static Dictionary<string, Account> ActiveAccounts;
        public static Dictionary<string, Channel> ActiveChannels;
        public static List<ClientState> ActiveClients;
        public static IPAddress DefaultInterface { get; private set; }
        public static int DefaultPort { get; private set; }
        public static TcpListener Listener;

        public static void Initialize()
        {
            AccountsDb = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            ActiveAccounts = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            ActiveChannels = new Dictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);
            ActiveClients = new List<ClientState>();

            // Channel adds itself to ActiveChannels during instantiation.
            new Channel(Channel.TheVoid, Channel.TheVoidFlags, -1);
            new Channel("Backstage", Channel.Flags.Public | Channel.Flags.Restricted, -1, "Abandon hope, all ye who enter here...");
            new Channel("Open Tech Support", Channel.Flags.Public | Channel.Flags.TechSupport, -1);
            new Channel("Blizzard Tech Support", Channel.Flags.Public | Channel.Flags.TechSupport | Channel.Flags.Moderated, -1);
            new Channel("Town Square", Channel.Flags.Public, 200, "Welcome and enjoy your stay!");

            DefaultInterface = IPAddress.Any;
            DefaultPort = 6112;

            Listener = new TcpListener(DefaultInterface, DefaultPort);

            Listener.ExclusiveAddressUse = false;
            Listener.Server.NoDelay = true;
            Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); // SO_KEEPALIVE
            try {
                Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            } catch (SocketException ex) {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Server, $"Unable to set linger option on listening socket: {ex.Message}");
            }
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
                _ => "Unknown (0x" + ((byte)protocolType).ToString("X2") + ")",
            };
        }
    }
}
