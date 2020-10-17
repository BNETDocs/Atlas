using System;

namespace Atlasd.Battlenet.Exceptions
{
    class GameProtocolViolationException : GameProtocolException
    {
        public GameProtocolViolationException(ClientState client) : base(client) { }
        public GameProtocolViolationException(ClientState client, string message) : base(client, message) { }
        public GameProtocolViolationException(ClientState client, string message, Exception innerException) : base(client, message, innerException) { }
    }
}
