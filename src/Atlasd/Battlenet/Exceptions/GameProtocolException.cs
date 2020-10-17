using System;

namespace Atlasd.Battlenet.Exceptions
{
    class GameProtocolException : ProtocolException
    {
        public GameProtocolException(ClientState client) : base(ProtocolType.Game, client) { }
        public GameProtocolException(ClientState client, string message) : base(ProtocolType.Game, client, message) { }
        public GameProtocolException(ClientState client, string message, Exception innerException) : base(ProtocolType.Game, client, message, innerException) { }
    }
}
