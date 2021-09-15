using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_FRIENDSPOSITION : Message
    {

        public SID_FRIENDSPOSITION()
        {
            Id = (byte)MessageIds.SID_FRIENDSPOSITION;
            Buffer = new byte[0];
        }

        public SID_FRIENDSPOSITION(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_FRIENDSPOSITION;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client");

            /**
             * (UINT8) Old entry number
             * (UINT8) New entry number
             */

            Buffer = new byte[2];

            using var m = new MemoryStream(Buffer);
            using var w = new BinaryWriter(m);

            w.Write((byte)context.Arguments["old"]);
            w.Write((byte)context.Arguments["new"]);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
