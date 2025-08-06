using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.MCP
{
    class MessageContext
    {
        public Dictionary<string, object> Arguments { get; protected set; }
        public RealmState RealmState { get; protected set; }
        public MessageDirection Direction { get; protected set; }

        public MessageContext(RealmState realmState, MessageDirection direction, Dictionary<string, dynamic> arguments = null)
        {
            Arguments = arguments;
            RealmState = realmState;
            Direction = direction;
        }
    }
}
