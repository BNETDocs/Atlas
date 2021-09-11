using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_SYSTEMINFO : Message
    {
        public SID_SYSTEMINFO()
        {
            Id = (byte)MessageIds.SID_SYSTEMINFO;
            Buffer = new byte[16];
        }

        public SID_SYSTEMINFO(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_SYSTEMINFO;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            if (Buffer.Length != 28)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 28 bytes");

            /**
             * (UINT32) Number of processors
             * (UINT32) Processor architecture
             * (UINT32) Processor level
             * (UINT32) Processor timing
             * (UINT32) Total physical memory
             * (UINT32) Total page file
             * (UINT32) Free disk space
             */

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var cpuCount      = r.ReadUInt32();
            var cpuArch       = r.ReadUInt32();
            var cpuLevel      = r.ReadUInt32();
            var cpuTiming     = r.ReadUInt32();
            var totalRAM      = r.ReadUInt32();
            var totalSwap     = r.ReadUInt32();
            var freeDiskSpace = r.ReadUInt32();

            return true;
        }
    }
}