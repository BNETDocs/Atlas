using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_GAMERESULT : Message
    {
        public SID_GAMERESULT()
        {
            Id = (byte)MessageIds.SID_GAMERESULT;
            Buffer = new byte[0];
        }

        public SID_GAMERESULT(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_GAMERESULT;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            /**
             * (UINT32) Game type
             * (UINT32) Number of results - always 8
             * (UINT32) [8] Results
             * (STRING) [8] Game players - always 8
             * (STRING) Map name
             * (STRING) Player score
             */

            if (Buffer.Length < 10)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 10 bytes");

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var gameType = r.ReadUInt32();
            var resultCount = r.ReadUInt32();
            var results = new List<UInt32>();
            var players = new List<byte[]>();

            for (var i = 0; i < resultCount; i++)
            {
                results.Add(r.ReadUInt32());
            }

            for (var i = 0; i < resultCount; i++)
            {
                players.Add(r.ReadByteString());
            }

            var mapName = r.ReadByteString();
            var playerScore = r.ReadByteString();

            return true;
        }
    }
}
