using System.Net;
using System.Net.Sockets;

namespace Atlas.Battlenet.Sockets
{
    class Bnftp : TcpClient
    {
        public Bnftp() : base(Battlenet.ProtocolType.BNFTP) { }
        public Bnftp(IPEndPoint localEP) : base(Battlenet.ProtocolType.BNFTP, localEP) { }
        public Bnftp(AddressFamily family) : base(Battlenet.ProtocolType.BNFTP, family) { }
        public Bnftp(string hostname, int port) : base(Battlenet.ProtocolType.BNFTP, hostname, port) { }
    }
}
