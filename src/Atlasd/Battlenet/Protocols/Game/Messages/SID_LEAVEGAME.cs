using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LEAVEGAME : Message
    {
        public SID_LEAVEGAME()
        {
            Id = (byte)MessageIds.SID_LEAVEGAME;
            Buffer = new byte[0];
        }

        public SID_LEAVEGAME(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_LEAVEGAME;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            if (context.Client == null || !context.Client.Connected || context.Client.GameState == null) return false;

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (Buffer.Length != 0)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 0 bytes, got {Buffer.Length}");

            if (!Product.IsDiabloII(context.Client.GameState.Product))
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} received but client not identified as Diablo II");

            // TODO: Implement action to take from receiving SID_LEAVEGAME.
            return true;
        }
    }
}
