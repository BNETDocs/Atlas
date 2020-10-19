using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_NULL : Message
    {
        public SID_NULL()
        {
            Id = (byte)MessageIds.SID_NULL;
            Buffer = new byte[0];
        }

        public SID_NULL(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_NULL;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_NULL ({4 + Buffer.Length} bytes)");

            if (Buffer.Length != 0)
                throw new GameProtocolViolationException(context.Client, "SID_NULL buffer must be 0 bytes");

            return true;
        }
    }
}
