using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CLICKAD : Message
    {
        public SID_CLICKAD()
        {
            Id = (byte)MessageIds.SID_CLICKAD;
            Buffer = new byte[0];
        }

        public SID_CLICKAD(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CLICKAD;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            if (Buffer.Length != 8)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 8 bytes");

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var adId = r.ReadUInt32();
            var requestType = r.ReadUInt32();

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Ad [Id: 0x{adId:X8}] was clicked!");

            return true;
        }
    }
}
