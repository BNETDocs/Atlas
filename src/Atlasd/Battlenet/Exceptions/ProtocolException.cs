using System;

namespace Atlasd.Battlenet.Exceptions
{
    class ProtocolException : ClientException
    {
        public ProtocolType ProtocolType { get; protected set; }

        public ProtocolException(ProtocolType protocolType, ClientState client) : base(client)
        {
            ProtocolType = protocolType;
        }

        public ProtocolException(ProtocolType protocolType, ClientState client, string message) : base(client, message)
        {
            ProtocolType = protocolType;
        }

        public ProtocolException(ProtocolType protocolType, ClientState client, string message, Exception innerException) : base(client, message, innerException)
        {
            ProtocolType = protocolType;
        }
    }
}
