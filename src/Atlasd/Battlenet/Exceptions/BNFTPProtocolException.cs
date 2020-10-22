using System;

namespace Atlasd.Battlenet.Exceptions
{
    class BNFTPProtocolException : ProtocolException
    {
        public BNFTPProtocolException(ClientState client) : base(Battlenet.ProtocolType.Types.BNFTP, client) { }
        public BNFTPProtocolException(ClientState client, string message) : base(Battlenet.ProtocolType.Types.BNFTP, client, message) { }
        public BNFTPProtocolException(ClientState client, string message, Exception innerException) : base(Battlenet.ProtocolType.Types.BNFTP, client, message, innerException) { }
    }
}
