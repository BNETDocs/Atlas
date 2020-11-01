using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Drawing.Imaging;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LOGONCHALLENGEEX : Message
    {

        public SID_LOGONCHALLENGEEX()
        {
            Id = (byte)MessageIds.SID_LOGONCHALLENGEEX;
            Buffer = new byte[8];
        }

        public SID_LOGONCHALLENGEEX(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_LOGONCHALLENGEEX;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_LOGONCHALLENGEEX ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, "SID_LOGONCHALLENGEEX must be sent from server to client");

            if (Buffer.Length != 8)
                throw new GameProtocolViolationException(context.Client, "SID_LOGONCHALLENGEEX buffer must be 8 bytes");

            var m = new MemoryStream(Buffer);
            var w = new BinaryWriter(m);

            w.Write((UInt32)context.Client.GameState.UDPToken);
            w.Write((UInt32)context.Client.GameState.ServerToken);

            w.Close();
            m.Close();

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_LOGONCHALLENGEEX ({4 + Buffer.Length} bytes)");
            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
