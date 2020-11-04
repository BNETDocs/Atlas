using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CHECKDATAFILE2 : Message
    {
        public enum Statuses : UInt32
        {
            Unapproved = 0,
            Approved = 1,
            LadderApproved = 2,
        };

        public SID_CHECKDATAFILE2()
        {
            Id = (byte)MessageIds.SID_CHECKDATAFILE2;
            Buffer = new byte[0];
        }

        public SID_CHECKDATAFILE2(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CHECKDATAFILE2;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CHECKDATAFILE2 ({4 + Buffer.Length} bytes)");

                        /**
                         * (UINT32) File size
                         * (UINT8) [20] File checksum (standard SHA-1)
                         * (STRING) File name
                         */

                        if (Buffer.Length < 25)
                            throw new GameProtocolViolationException(context.Client, "SID_CHECKDATAFILE2 buffer must be at least 25 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var fileSize = r.ReadUInt32();
                        var fileChecksum = r.ReadBytes(20);
                        var fileName = Encoding.UTF8.GetString(r.ReadByteString());

                        var status = Statuses.Unapproved;

                        return new SID_CHECKDATAFILE2().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){{ "status", status }}));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Status
                         */

                        Buffer = new byte[4];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)context.Arguments["status"]);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CHECKDATAFILE2 ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
