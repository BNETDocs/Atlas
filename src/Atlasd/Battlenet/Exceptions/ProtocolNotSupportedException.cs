using System;

namespace Atlasd.Battlenet.Exceptions
{
    class ProtocolNotSupportedException : ClientException
    {
        public ProtocolType ProtocolType { get; private set; }

        public ProtocolNotSupportedException(ProtocolType protocolType, ClientState client, string message = "Unsupported protocol") : base(client, message)
        {
            ProtocolType = protocolType;
        }

        public ProtocolNotSupportedException(ProtocolType protocolType, ClientState client, string message, Exception innerException) : base(client, message, innerException)
        {
            ProtocolType = protocolType;
        }
    }
}
