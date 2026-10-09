using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_NETGAMEPORT : Message
    {
        public SID_NETGAMEPORT()
        {
            Id = (byte)MessageIds.SID_NETGAMEPORT;
            Buffer = new byte[0];
        }

        public SID_NETGAMEPORT(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_NETGAMEPORT;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context == null || context.Client == null || !context.Client.Connected || context.Client.GameState == null) return false;

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} is a client-to-server message");

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (Buffer.Length != 2)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 2 bytes");

            /**
             * (UINT16) Port
             */

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            context.Client.GameState.GameDataPort = r.ReadUInt16();
            return true;
        }
    }
}
