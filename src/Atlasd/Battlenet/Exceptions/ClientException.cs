using System;
using System.Net;

namespace Atlasd.Battlenet.Exceptions
{
    class ClientException : Exception
    {
        public ClientState Client { get; private set; }

        public ClientException(ClientState client) : base()
        {
            Client = client;
        }

        public ClientException(ClientState client, string message) : base(message)
        {
            Client = client;
        }

        public ClientException(ClientState client, string message, Exception innerException) : base(message, innerException)
        {
            Client = client;
        }
    }
}
