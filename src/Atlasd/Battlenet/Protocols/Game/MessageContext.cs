using Atlasd.Battlenet.Sockets;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game
{
    class MessageContext
    {
        public Dictionary<string, object> Arguments { get; protected set; }
        public TcpClient Client { get; protected set; }
        public MessageDirection Direction { get; protected set; }

        public MessageContext(TcpClient client, MessageDirection direction, Dictionary<string, object> arguments = null)
        {
            Arguments = arguments;
            Client = client;
            Direction = direction;
        }
    }
}
