using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game
{
    class MessageContext
    {
        public Dictionary<string, object> Arguments { get; protected set; }
        public ClientState Client { get; protected set; }
        public MessageDirection Direction { get; protected set; }

        public MessageContext(ClientState client, MessageDirection direction, Dictionary<string, object> arguments = null)
        {
            Arguments = arguments;
            Client = client;
            Direction = direction;
        }
    }
}
