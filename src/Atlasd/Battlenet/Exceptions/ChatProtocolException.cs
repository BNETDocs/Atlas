using System;

namespace Atlasd.Battlenet.Exceptions
{
    class ChatProtocolException : ProtocolException
    {
        public ChatProtocolException(ClientState client) : base(Battlenet.ProtocolType.Types.Chat, client) { }
        public ChatProtocolException(ClientState client, string message) : base(Battlenet.ProtocolType.Types.Chat, client, message) { }
        public ChatProtocolException(ClientState client, string message, Exception innerException) : base(Battlenet.ProtocolType.Types.Chat, client, message, innerException) { }
    }
}
