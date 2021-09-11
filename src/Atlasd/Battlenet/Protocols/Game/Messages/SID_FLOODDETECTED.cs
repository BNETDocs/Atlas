using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_FLOODDETECTED : Message
    {

        public SID_FLOODDETECTED()
        {
            Id = (byte)MessageIds.SID_FLOODDETECTED;
            Buffer = new byte[0];
        }

        public SID_FLOODDETECTED(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_FLOODDETECTED;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client");

            if (Buffer.Length != 0)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 0 bytes");

            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
