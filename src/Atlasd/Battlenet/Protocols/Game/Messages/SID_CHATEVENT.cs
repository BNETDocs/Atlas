using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.Net.Mime;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CHATEVENT : Message
    {
        public SID_CHATEVENT()
        {
            Id = (byte)MessageIds.SID_CHATEVENT;
            Buffer = new byte[26];
        }

        public SID_CHATEVENT(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CHATEVENT;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction == MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, "Client is not allowed to send SID_CHATEVENT");

            var chatEvent = (ChatEvent)context.Arguments["chatEvent"];

            Buffer = chatEvent.ToByteArray(context.Client.ProtocolType);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, string.Format("[{0}] SID_CHATEVENT: {1} ({2:D} bytes)", Common.DirectionToString(context.Direction), ChatEvent.EventIdToString(chatEvent.EventId), 4 + Buffer.Length));

            return true;
        }
    }
}
