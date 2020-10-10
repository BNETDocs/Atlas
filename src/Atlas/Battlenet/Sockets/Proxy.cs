using System.Net;
using System.Net.Sockets;

namespace Atlas.Battlenet.Sockets
{
    abstract class Proxy : TcpClient
    {
        protected ProxyType proxyType;

        public enum ProxyType
        {
            Disabled = 0,
            Socks4 = 1,
            Socks5 = 2,
            HTTP = 3,
        }

        public Proxy(ProxyType proxyType, ProtocolType protocolType) : base(protocolType) { this.proxyType = proxyType; }
        public Proxy(ProxyType proxyType, ProtocolType protocolType, IPEndPoint localEP) : base(protocolType, localEP) { this.proxyType = proxyType; }
        public Proxy(ProxyType proxyType, ProtocolType protocolType, AddressFamily family) : base(protocolType, family) { this.proxyType = proxyType; }
        public Proxy(ProxyType proxyType, ProtocolType protocolType, string hostname, int port) : base(protocolType, hostname, port) { this.proxyType = proxyType; }
    }
}
