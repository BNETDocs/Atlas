using System;

namespace Atlasd.Battlenet.Exceptions
{
    class ChatProtocolException : ProtocolException
    {
        public ChatProtocolException(ClientState client) : base(ProtocolType.Chat, client) { }
        public ChatProtocolException(ClientState client, string message) : base(ProtocolType.Chat, client, message) { }
        public ChatProtocolException(ClientState client, string message, Exception innerException) : base(ProtocolType.Chat, client, message, innerException) { }
    }
}
