using System;

namespace Atlasd.Battlenet.Exceptions
{
    class GameProtocolException : ProtocolException
    {
        public GameProtocolException(ClientState client) : base(Battlenet.ProtocolType.Types.Game, client) { }
        public GameProtocolException(ClientState client, string message) : base(Battlenet.ProtocolType.Types.Game, client, message) { }
        public GameProtocolException(ClientState client, string message, Exception innerException) : base(Battlenet.ProtocolType.Types.Game, client, message, innerException) { }
    }
}
