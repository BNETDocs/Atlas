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
                var t = (UInt32)context.Arguments["token"];

                Buffer = new byte[4];

                using var _m = new MemoryStream(Buffer);
                using var _w = new BinaryWriter(_m);
                _w.Write(t);
            }

            if (Buffer.Length != 4)
                throw new GameProtocolViolationException(context.Client, "SID_PING buffer must be 4 bytes");

            bool autoRefreshPings = false;
            try
            {
                Settings.State.RootElement.TryGetProperty("battlenet", out var battlenetJson);
                battlenetJson.TryGetProperty("emulation", out var emulationJson);
                emulationJson.TryGetProperty("auto_refresh_pings", out var autoRefreshPingsJson);
                autoRefreshPings = autoRefreshPingsJson.GetBoolean();
            }
            catch (Exception ex)
            {
                if (!(ex is InvalidOperationException || ex is ArgumentNullException))
                {
                    throw ex;
                }

                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, "Setting [battlenet] -> [emulation] -> [auto_refresh_pings] is invalid; check value");
            }

            if (!autoRefreshPings) return true;

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);
            var token = r.ReadUInt32();

            lock (context.Client.GameState)
            {
                var serverToken = context.Client.GameState.PingToken;

                if (context.Direction == MessageDirection.ClientToServer && token == serverToken)
                {
                    var delta = DateTime.Now - context.Client.GameState.PingDelta;
                    context.Client.GameState.Ping = (int)Math.Round(delta.TotalMilliseconds);

                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Ping: {context.Client.GameState.Ping}ms");

                    if (context.Client.GameState.ActiveChannel != null)
                    {
                        context.Client.GameState.ActiveChannel.UpdateUser(context.Client.GameState, context.Client.GameState.Ping);
                    }
                }
            }

            return true;
        }
    }
}
