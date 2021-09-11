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
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Arguments != null && context.Arguments.ContainsKey("token"))
            {
                var t = (UInt32)context.Arguments["token"];

                Buffer = new byte[4];

                using var _m = new MemoryStream(Buffer);
                using var _w = new BinaryWriter(_m);
                _w.Write(t);
            }

            if (Buffer.Length != 4)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes");

            if (context.Client == null || !context.Client.Connected) return false;

            var delta = DateTime.Now - context.Client.GameState.PingDelta;

            var autoRefreshPings = Settings.GetBoolean(new string[] { "battlenet", "emulation", "auto_refresh_pings" }, false);

            if (!autoRefreshPings && context.Client.GameState.Ping != -1) return true;

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);
            var token = r.ReadUInt32();

            if (!(context.Direction == MessageDirection.ClientToServer && token == context.Client.GameState.PingToken)) return true;

            context.Client.GameState.Ping = (int)Math.Round(delta.TotalMilliseconds);

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Ping: {context.Client.GameState.Ping}ms");

            if (context.Client.GameState.ActiveChannel != null)
            {
                context.Client.GameState.ActiveChannel.UpdateUser(context.Client.GameState, context.Client.GameState.Ping);
            }

            return true;
        }
    }
}
