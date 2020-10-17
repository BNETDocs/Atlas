using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

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
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_PING (" + (4 + Buffer.Length) + " bytes)");

            if (Buffer.Length != 4)
                throw new GameProtocolViolationException(context.Client, "SID_PING buffer must be 4 bytes");

            if (context.Arguments.ContainsKey("token"))
            {
                var m = new MemoryStream(Buffer);
                var w = new BinaryWriter(m);

                w.Write((UInt32)context.Arguments["token"]);

                w.Close();
                m.Close();
            }

            var token = (UInt32)((Buffer[3] << 24) + (Buffer[2] << 16) + (Buffer[1] << 8) + Buffer[0]);

            if (token != context.Client.GameState.PingToken)
                throw new GameProtocolViolationException(context.Client, "SID_PING token mismatch with server [Token: 0x" + token.ToString("X8") + "] [Server: 0x" + context.Client.GameState.PingToken.ToString("X8") + "]");

            // Refresh the ping token that has now been used:
            lock (context.Client.GameState)
            {
                context.Client.GameState.PingToken = (uint)new Random().Next(0, 0x7FFFFFFF);
            }

            var delta = DateTime.Now - context.Client.GameState.PingDelta;
            context.Client.GameState.Ping = (int)Math.Round(delta.TotalMilliseconds);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_PING Token: [0x" + token.ToString("X8") + "] Ping: " + context.Client.GameState.Ping + "ms");

            if (context.Client.GameState.ActiveChannel != null)
                context.Client.GameState.ActiveChannel.UpdateUser(context.Client.GameState);

            return true;
        }
    }
}
