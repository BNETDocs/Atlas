using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_REPORTVERSION : Message
    {
        public enum ResultIds : UInt32
        {
            Failed = 0,
            OldGame = 1,
            Success = 2,
            Reinstall = 3,
        }

        public SID_REPORTVERSION()
        {
            Id = (byte)MessageIds.SID_REPORTVERSION;
            Buffer = new byte[6];
        }

        public SID_REPORTVERSION(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_REPORTVERSION;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_REPORTVERSION ({4 + Buffer.Length} bytes)");

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        if (Buffer.Length < 21)
                            throw new GameProtocolViolationException(context.Client, "SID_REPORTVERSION buffer must be at least 21 bytes");

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        context.Client.GameState.Platform = (Platform.PlatformCode)r.ReadUInt32();
                        context.Client.GameState.Product = (Product.ProductCode)r.ReadUInt32();
                        context.Client.GameState.Version.VersionByte = r.ReadUInt32();
                        context.Client.GameState.Version.EXERevision = r.ReadUInt32();
                        context.Client.GameState.Version.EXEChecksum = r.ReadUInt32();
                        context.Client.GameState.Version.EXEInformation = r.ReadString();

                        r.Close();
                        m.Close();

                        return new SID_REPORTVERSION().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Result
                         * (STRING) Filename
                         *  (UINT8) Unknown (0)
                         */

                        var filename = "ver-IX86-1.mpq";

                        Buffer = new byte[6 + Encoding.ASCII.GetByteCount(filename)];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)ResultIds.Success);
                        w.Write((string)filename);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_REPORTVERSION ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
