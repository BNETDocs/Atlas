using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_GETFILETIME : Message
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

        public SID_GETFILETIME()
        {
            Id = (byte)MessageIds.SID_GETFILETIME;
            Buffer = new byte[0];
        }

        public SID_GETFILETIME(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_GETFILETIME;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_GETFILETIME ({4 + Buffer.Length} bytes)");

                        /**
                         * (UINT32) Request ID
                         * (UINT32) Unknown
                         * (STRING) Filename
                         */

                        if (Buffer.Length < 9)
                            throw new GameProtocolViolationException(context.Client, "SID_GETFILETIME buffer must be at least 9 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var requestId = r.ReadUInt32();
                        var unknown = r.ReadUInt32();
                        var filename = r.ReadString();

                        return new SID_GETFILETIME().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {
                            { "requestId", requestId }, { "unknown", unknown }, { "filename", filename }
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Request ID
                         * (UINT32) Unknown
                         * (FILETIME) Last update time
                         * (STRING) Filename 
                         */

                        var requestId = (UInt32)context.Arguments["requestId"];
                        var unknown = (UInt32)context.Arguments["unknown"];
                        var filetime = (UInt64)0;
                        var filename = (string)context.Arguments["filename"];

                        Buffer = new byte[17 + Encoding.UTF8.GetByteCount(filename)];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)requestId);
                        w.Write((UInt32)unknown);
                        w.Write((UInt64)filetime);
                        w.Write((string)filename);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_GETFILETIME ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
