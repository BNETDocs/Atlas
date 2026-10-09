using System;

namespace Atlasd.Battlenet.Exceptions
{
    class RealmProtocolException : ProtocolException
    {
        public RealmProtocolException(ClientState client) : base(Battlenet.ProtocolType.Types.Game, client) { }
        public RealmProtocolException(ClientState client, string message) : base(Battlenet.ProtocolType.Types.Game, client, message) { }
        public RealmProtocolException(ClientState client, string message, Exception innerException) : base(Battlenet.ProtocolType.Types.Game, client, message, innerException) { }
    }
}
