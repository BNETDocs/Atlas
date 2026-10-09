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

        private static bool Reply(MessageContext context, byte subcommand, UInt32? status)
        {
            var buffer = new byte[status.HasValue ? 9 : 5];
            using (var m = new MemoryStream(buffer))
            using (var w = new BinaryWriter(m))
            {
                w.Write(subcommand);
                w.Write(context.Client.GameState.GameSearchCookie);
                if (status.HasValue) w.Write(status.Value);
            }

            return new SID_WARCRAFTGENERAL().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "buffer", buffer }}));
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction == MessageDirection.ServerToClient)
            {
                Buffer = (byte[])context.Arguments["buffer"];
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} subcommand {Buffer[0]:X2} ({4 + Buffer.Length} bytes)");
                context.Client.Send(ToByteArray(context.Client.ProtocolType));
                return true;
            }

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Client.GameState == null || !Product.IsWarcraftIII(context.Client.GameState.Product))
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} is Warcraft III game client exclusive");

            if (Buffer.Length < 1)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 1 byte");

            byte subcommand;
            using (var m = new MemoryStream(Buffer))
            using (var r = new BinaryReader(m))
            {
                subcommand = r.ReadByte();

                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} received subcommand {subcommand:X2}");

                switch ((SubCommands)subcommand)
                {
                    case SubCommands.WID_GAMESEARCH:
                        {
                            /**
                             * Warcraft III demo layout:
                             *   (UINT8) Subcommand
                             *  (UINT32) Cookie
                             *  (UINT32) Unknown
                             *   (UINT8) Unknown
                             *   (UINT8) Unknown
                             *  (UINT32) Unknown
                             *   (UINT8) Length of the remaining data (8)
                             *  (UINT32) Tick count
                             *  (UINT32) Race
                             */

                            if (Buffer.Length < 16)
                                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} game search buffer must be at least 16 bytes");

                            context.Client.GameState.GameSearchCookie = r.ReadUInt32();
                            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Game search requested (cookie 0x{context.Client.GameState.GameSearchCookie:X8}): {BitConverter.ToString(Buffer)}");

                            return Reply(context, subcommand, 0);
                        }
                    case SubCommands.WID_CANCELSEARCH:
                        return Reply(context, subcommand, null);
                }
            }

            return true;
        }
    }
}
