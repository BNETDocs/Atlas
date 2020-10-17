using System;

namespace Atlasd.Battlenet.Exceptions
{
    class BNFTPProtocolException : ProtocolException
    {
        public BNFTPProtocolException(ClientState client) : base(ProtocolType.BNFTP, client) { }
        public BNFTPProtocolException(ClientState client, string message) : base(ProtocolType.BNFTP, client, message) { }
        public BNFTPProtocolException(ClientState client, string message, Exception innerException) : base(ProtocolType.BNFTP, client, message, innerException) { }
    }
}
