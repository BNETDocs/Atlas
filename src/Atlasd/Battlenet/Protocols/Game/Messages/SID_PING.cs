using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_PING : Message
    {
        public SID_PING()
        {
            Id = (byte)MessageIds.SID_PING;
            Buffer = new byte[4];
        }

        public SID_PING(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_PING;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_PING (" + (4 + Buffer.Length) + " bytes)");

            if (Buffer.Length != 4)
                throw new ProtocolViolationException(context.Client.ProtocolType, "SID_PING buffer must be 4 bytes");

            var token = (UInt32)((Buffer[3] << 24) + (Buffer[2] << 16) + (Buffer[1] << 8) + Buffer[0]);

            var delta = DateTime.Now - context.Client.GameState.PingDelta;
            context.Client.GameState.Ping = (int)Math.Round(delta.TotalMilliseconds);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_PING Token: [0x" + token.ToString("X8") + "] Ping: " + context.Client.GameState.Ping + "ms");

            return true;
        }
    }
}
