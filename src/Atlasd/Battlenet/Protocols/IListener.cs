using System.Net;
using System.Net.Sockets;

namespace Atlasd.Battlenet.Protocols
{
    interface IListener
    {
        public IPEndPoint LocalEndPoint { get; }
        public bool IsListening { get; }
        public Socket Socket { get; }

        public void Close();
        public void SetLocalEndPoint(IPEndPoint endp);
        public void Start();
        public void Stop();
    }
}
