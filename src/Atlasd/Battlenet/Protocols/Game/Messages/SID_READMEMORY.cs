using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_READMEMORY : Message
    {
        public SID_READMEMORY()
        {
            Id = (byte)MessageIds.SID_READMEMORY;
            Buffer = new byte[0];
        }

        public SID_READMEMORY(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_READMEMORY;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        /**
                         * (UINT32) Request ID
                         * (VOID) Memory
                         */

                        if (Buffer.Length < 4)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 4 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var requestId = r.ReadUInt32();
                        var data = r.ReadBytes((int)(r.BaseStream.Length - r.BaseStream.Position));

                        // We don't use this. Why did the client send us this ??

                        break;
                    }
                case MessageDirection.ServerToClient:
                    {
                        var requestId = (UInt32)context.Arguments["requestId"];
                        var address = (UInt32)context.Arguments["address"];
                        var length = (UInt32)context.Arguments["length"];

                        /**
                         * (UINT32) Request ID
                         * (UINT32) Address
                         * (UINT32) Length
                         */

                        Buffer = new byte[12];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)requestId);
                        w.Write((UInt32)address);
                        w.Write((UInt32)length);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
