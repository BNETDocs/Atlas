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
            Buffer = new byte[0];
        }

        public SID_PING(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_PING;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_PING ({4 + Buffer.Length} bytes)");

            if (context.Arguments != null && context.Arguments.ContainsKey("token"))
            {
                // cheaper to do the math here than instantiate BinaryReader/BinaryWriter and MemoryStream objects
                var t     = (UInt32)context.Arguments["token"];
                Buffer    = new byte[4];
                Buffer[0] = (byte)(t >> 24);
                Buffer[1] = (byte)(t >> 16);
                Buffer[2] = (byte)(t >> 8);
                Buffer[3] = (byte)t;
            }

            if (Buffer.Length != 4)
                throw new GameProtocolViolationException(context.Client, "SID_PING buffer must be 4 bytes");

            var token = (UInt32)((Buffer[3] << 24) + (Buffer[2] << 16) + (Buffer[1] << 8) + Buffer[0]);
            var serverToken = (UInt32)0;

            lock (context.Client.GameState) serverToken = context.Client.GameState.PingToken;

            if (context.Direction == MessageDirection.ClientToServer && token == serverToken)
            {
                lock (context.Client.GameState)
                {
                    var delta = DateTime.Now - context.Client.GameState.PingDelta;
                    context.Client.GameState.Ping = (int)Math.Round(delta.TotalMilliseconds);
                }

                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Ping: {context.Client.GameState.Ping}ms");

                if (context.Client.GameState.ActiveChannel != null)
                    context.Client.GameState.ActiveChannel.UpdateUser(context.Client.GameState, context.Client.GameState.Ping);
            }

            return true;
        }
    }
}
