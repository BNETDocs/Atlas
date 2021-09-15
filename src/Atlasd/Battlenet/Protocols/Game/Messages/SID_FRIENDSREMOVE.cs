using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_FRIENDSREMOVE : Message
    {

        public SID_FRIENDSREMOVE()
        {
            Id = (byte)MessageIds.SID_FRIENDSREMOVE;
            Buffer = new byte[0];
        }

        public SID_FRIENDSREMOVE(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_FRIENDSREMOVE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client");

            /**
             * (UINT8) Entry number
             */

            Buffer = new byte[1];

            using var m = new MemoryStream(Buffer);
            using var w = new BinaryWriter(m);

            w.Write((byte)context.Arguments["friend"]);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
