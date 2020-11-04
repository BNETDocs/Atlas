using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CHECKDATAFILE : Message
    {
        public enum RequestIds : UInt32
        {
            TermsOfService_usa = 0x01,
            BnServerListW3 = 0x03,
            TermsOfService_USA = 0x1A,
            BnServerList = 0x1B,
            IconsSC = 0x1D,
            BnServerListD2 = 0x80000004,
            ExtraOptionalWorkIX86 = 0x80000005,
            ExtraRequiredWorkIX86 = 0x80000006,
        };

        public SID_CHECKDATAFILE()
        {
            Id = (byte)MessageIds.SID_CHECKDATAFILE;
            Buffer = new byte[0];
        }

        public SID_CHECKDATAFILE(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CHECKDATAFILE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CHECKDATAFILE ({4 + Buffer.Length} bytes)");

                        /**
                         * (UINT8) [20] File Checksum (XSHA-1)
                         * (STRING) Filename
                         */

                        if (Buffer.Length < 21)
                            throw new GameProtocolViolationException(context.Client, "SID_CHECKDATAFILE buffer must be at least 21 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var fileChecksum = r.ReadBytes(20);
                        var fileName = r.ReadByteString();

                        return new SID_CHECKDATAFILE().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Status:
                         *     0 - Rejected
                         *     1 - Approved
                         *     2 - Ladder Approved
                         */

                        Buffer = new byte[4];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)0); // Reject everything from this deprecated message.

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CHECKDATAFILE ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
