using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_DISPLAYAD : Message
    {
        public SID_DISPLAYAD()
        {
            Id = (byte)MessageIds.SID_DISPLAYAD;
            Buffer = new byte[0];
        }

        public SID_DISPLAYAD(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_DISPLAYAD;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_DISPLAYAD ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, "SID_DISPLAYAD must be sent from client to server");

            if (Buffer.Length < 14)
                throw new GameProtocolViolationException(context.Client, "SID_DISPLAYAD buffer must be at least 14 bytes");

            var m = new MemoryStream(Buffer);
            var r = new BinaryReader(m);

            var platformId = r.ReadUInt32();
            var productId = r.ReadUInt32();
            var adId = r.ReadUInt32();
            var adFilename = r.ReadString();
            var adUrl = r.ReadString();

            r.Close();
            m.Close();

            return true;
        }
    }
}
