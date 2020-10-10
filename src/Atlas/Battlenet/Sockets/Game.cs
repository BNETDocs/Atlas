using System.Net;
using System.Net.Sockets;

namespace Atlas.Battlenet.Sockets
{
    class Game : TcpClient
    {
        public Game() : base(Battlenet.ProtocolType.Game) { }
        public Game(IPEndPoint localEP) : base(Battlenet.ProtocolType.Game, localEP) { }
        public Game(AddressFamily family) : base(Battlenet.ProtocolType.Game, family) { }
        public Game(string hostname, int port) : base(Battlenet.ProtocolType.Game, hostname, port) { }
    }
}
