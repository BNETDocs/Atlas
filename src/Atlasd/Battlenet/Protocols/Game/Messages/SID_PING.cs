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
            if (context.Client == null || !context.Client.Connected || context.Client.GameState == null) return false;
            var gameState = context.Client.GameState;

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                {
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                    if (Buffer.Length != 4)
                        throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes");

                    using var m = new MemoryStream(Buffer);
                    using var r = new BinaryReader(m);
                    var token = r.ReadUInt32();

                    gameState.LastPong = DateTime.Now;
                    var delta = gameState.LastPong - gameState.LastPing;

                    var autoRefreshPings = Settings.GetBoolean(new string[] { "battlenet", "emulation", "auto_refresh_pings" }, false);
                    if (gameState.Ping != -1 && !autoRefreshPings) return true;

                    if (gameState.ActiveChannel == null)
                        gameState.Ping = (int)Math.Round(delta.TotalMilliseconds);
                    else
                        gameState.ActiveChannel.UpdateUser(gameState, gameState.Ping);

                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Ping: {gameState.Ping}ms");
                    return true;
                }
                case MessageDirection.ServerToClient:
                {
                    var token = (UInt32)(context.Arguments.ContainsKey("token") ? context.Arguments["token"] : (new Random()).Next());

                    Buffer = new byte[4];
                    using var m = new MemoryStream(Buffer);
                    using var w = new BinaryWriter(m);
                    w.Write((UInt32)token);

                    gameState.LastPing = DateTime.Now;
                    gameState.PingToken = token;

                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                    context.Client.Send(ToByteArray(context.Client.ProtocolType));
                    return true;
                }
                default: return false;
            }
        }
    }
}
