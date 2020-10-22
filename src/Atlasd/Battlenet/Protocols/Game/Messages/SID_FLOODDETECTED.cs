using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Drawing.Imaging;
using System.IO;

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
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_FLOODDETECTED ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, "SID_FLOODDETECTED must be sent from server to client");

            if (Buffer.Length != 0)
                throw new GameProtocolViolationException(context.Client, "SID_FLOODDETECTED buffer must be 0 bytes");

            context.Client.Send(ToByteArray());
            return true;
        }
    }
}
