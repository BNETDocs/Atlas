using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_GETADVLISTEX : Message
    {
        public SID_GETADVLISTEX()
        {
            Id = (byte)MessageIds.SID_GETADVLISTEX;
            Buffer = new byte[0];
        }

        public SID_GETADVLISTEX(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_GETADVLISTEX;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_GETADVLISTEX ({4 + Buffer.Length} bytes)");

                        /**
                         * (UINT16) Game Type
                         * (UINT16) Sub Game Type
                         * (UINT32) Viewing Filter
                         * (UINT32) Reserved (0)
                         * (UINT32) Number of Games
                         * (STRING) Game Name
                         * (STRING) Game Password
                         * (STRING) Game Statstring
                         */

                        if (Buffer.Length < 19)
                            throw new GameProtocolViolationException(context.Client, "SID_GETADVLISTEX buffer must be at least 19 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var gameType = r.ReadUInt16();
                        var subGameType = r.ReadUInt16();
                        var viewingFilter = r.ReadUInt32();
                        var reserved = r.ReadUInt32();
                        var numberOfGames = r.ReadUInt32();
                        var gameName = r.ReadString();
                        var gamePassword = r.ReadString();
                        var gameStatstring = r.ReadString();

                        return true;
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Number of games
                         *
                         * If count is 0:
                         *    (UINT32) Status
                         *
                         * Otherwise, games are listed thus:
                         *    For each list item:
                         *       (UINT32) Game settings
                         *       (UINT32) Language ID
                         *       (UINT16) Address Family (Always AF_INET)
                         *       (UINT16) Port
                         *       (UINT32) Host's IP
                         *       (UINT32) sin_zero (0)
                         *       (UINT32) sin_zero (0)
                         *       (UINT32) Game status
                         *       (UINT32) Elapsed time (in seconds)
                         *       (STRING) Game name
                         *       (STRING) Game password
                         *       (STRING) Game statstring
                         */

                        Buffer = new byte[8];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)0); // number of games
                        w.Write((UInt32)0); // status 0 = success

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_GETADVLISTEX ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
