using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Drawing.Imaging;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LOGONCHALLENGE : Message
    {

        public SID_LOGONCHALLENGE()
        {
            Id = (byte)MessageIds.SID_LOGONCHALLENGE;
            Buffer = new byte[4];
        }

        public SID_LOGONCHALLENGE(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_LOGONCHALLENGE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_LOGONCHALLENGE ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, "SID_LOGONCHALLENGE must be sent from server to client");

            if (Buffer.Length != 4)
                throw new GameProtocolViolationException(context.Client, "SID_LOGONCHALLENGE buffer must be 4 bytes");

            var m = new MemoryStream(Buffer);
            var w = new BinaryWriter(m);

            w.Write((UInt32)context.Client.GameState.ServerToken);

            w.Close();
            m.Close();

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_LOGONCHALLENGE ({4 + Buffer.Length} bytes)");
            context.Client.Send(ToByteArray());
            return true;
        }
    }
}
