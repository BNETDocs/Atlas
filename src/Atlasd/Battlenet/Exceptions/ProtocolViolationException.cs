using System;

namespace Atlasd.Battlenet.Exceptions
{
    class ProtocolViolationException : Exception
    {
        public ProtocolType ProtocolType { get; protected set; }

        public ProtocolViolationException(ProtocolType protocolType)
        {
            ProtocolType = protocolType;
        }

        public ProtocolViolationException(ProtocolType protocolType, string message) : base(message)
        {
            ProtocolType = protocolType;
        }

        public ProtocolViolationException(ProtocolType protocolType, string message, Exception innerException) : base(message, innerException)
        {
            ProtocolType = protocolType;
        }
    }
}
