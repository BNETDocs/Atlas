using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CLANINFO : Message
    {
        public SID_CLANINFO()
        {
            Id = (byte)MessageIds.SID_CLANINFO;
            Buffer = new byte[6];
        }

        public SID_CLANINFO(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CLANINFO;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client");

            /**
             *  (UINT8) Unknown (0)
             * (UINT32) Clan tag
             *  (UINT8) Rank
             */

            byte unknown = 0;
            byte[] tag = (byte[])context.Arguments["tag"];
            byte rank = (byte)context.Arguments["rank"];

            if (tag.Length != 4)
                throw new GameProtocolViolationException(context.Client, $"Clan tag must be exactly 4 bytes");

            Buffer = new byte[2 + tag.Length];

            using var m = new MemoryStream(Buffer);
            using var w = new BinaryWriter(m);

            w.Write(unknown);
            w.Write(tag);
            w.Write(rank);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
