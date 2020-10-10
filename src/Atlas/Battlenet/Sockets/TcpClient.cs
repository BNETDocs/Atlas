using System.Net;
using System.Net.Sockets;

namespace Atlas.Battlenet.Sockets
{
    abstract class TcpClient : System.Net.Sockets.TcpClient
    {
        protected Battlenet.ProtocolType protocolType;

        public TcpClient(Battlenet.ProtocolType protocolType) : base() { this.protocolType = protocolType; }
        public TcpClient(Battlenet.ProtocolType protocolType, IPEndPoint localEP) : base(localEP) { this.protocolType = protocolType; }
        public TcpClient(Battlenet.ProtocolType protocolType, AddressFamily family) : base(family) { this.protocolType = protocolType; }
        public TcpClient(Battlenet.ProtocolType protocolType, string hostname, int port) : base(hostname, port) { this.protocolType = protocolType; }
    }
}
