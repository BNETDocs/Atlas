using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_WARCRAFTGENERAL : Message
    {
        public enum SubCommands : byte
        {
            WID_GAMESEARCH = 0x00,
            WID_MAPLIST = 0x02,
            WID_CANCELSEARCH = 0x03,
            WID_USERRECORD = 0x04,
            WID_TOURNAMENT = 0x07,
            WID_CLANRECORD = 0x08,
            WID_ICONLIST = 0x09,
            WID_SETICON = 0x0A,
        };

        public SID_WARCRAFTGENERAL()
        {
            Id = (byte)MessageIds.SID_WARCRAFTGENERAL;
            Buffer = new byte[0];
        }

        public SID_WARCRAFTGENERAL(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_WARCRAFTGENERAL;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (Buffer.Length < 1)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 1 byte");

            byte subcommand;
            using (var m = new MemoryStream(Buffer))
            using (var r = new BinaryReader(m))
                subcommand = r.ReadByte();

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} received subcommand {subcommand:X2}");

            // TODO: Compare subcommand variable with SubCommands enum and do procedures, for now just ignore the client

            return true;
        }
    }
}
