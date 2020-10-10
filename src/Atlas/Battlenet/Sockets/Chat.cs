using System.Net;
using System.Net.Sockets;

namespace Atlas.Battlenet.Sockets
{
    class Chat : TcpClient
    {
        public Chat(bool useAlt = false) : base(useAlt ? Battlenet.ProtocolType.Chat_Alt : Battlenet.ProtocolType.Chat) { }
        public Chat(bool useAlt, IPEndPoint localEP) : base(useAlt ? Battlenet.ProtocolType.Chat_Alt : Battlenet.ProtocolType.Chat, localEP) { }
        public Chat(bool useAlt, AddressFamily family) : base(useAlt ? Battlenet.ProtocolType.Chat_Alt : Battlenet.ProtocolType.Chat, family) { }
        public Chat(bool useAlt, string hostname, int port) : base(useAlt ? Battlenet.ProtocolType.Chat_Alt : Battlenet.ProtocolType.Chat, hostname, port) { }
    }
}
