using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_STOPADV : Message
    {
        public SID_STOPADV()
        {
            Id = (byte)MessageIds.SID_STOPADV;
            Buffer = new byte[0];
        }

        public SID_STOPADV(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_STOPADV;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_STOPADV ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, "SID_STOPADV must be sent from client to server");

            if (Buffer.Length != 0)
                throw new GameProtocolViolationException(context.Client, "SID_STOPADV buffer must be 0 bytes");

            if (context.Client.GameState == null)
                throw new GameProtocolViolationException(context.Client, "SID_STOPADV was received without an active GameState");

            context.Client.GameState.StopGameAd();
            return true;
        }
    }
}
