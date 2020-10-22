using System;

namespace Atlasd.Battlenet.Exceptions
{
    class ProtocolException : ClientException
    {
        public ProtocolType.Types ProtocolType { get; protected set; }

        public ProtocolException(ProtocolType.Types protocolType, ClientState client) : base(client)
        {
            ProtocolType = protocolType;
        }

        public ProtocolException(ProtocolType.Types protocolType, ClientState client, string message) : base(client, message)
        {
            ProtocolType = protocolType;
        }

        public ProtocolException(ProtocolType.Types protocolType, ClientState client, string message, Exception innerException) : base(client, message, innerException)
        {
            ProtocolType = protocolType;
        }
    }
}
