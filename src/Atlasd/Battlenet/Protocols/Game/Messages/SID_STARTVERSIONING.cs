using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_STARTVERSIONING : Message
    {
        public SID_STARTVERSIONING()
        {
            Id = (byte)MessageIds.SID_STARTVERSIONING;
            Buffer = new byte[0];
        }

        public SID_STARTVERSIONING(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_STARTVERSIONING;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_STARTVERSIONING ({4 + Buffer.Length} bytes)");

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        /**
                         * (UINT32) Platform code
                         * (UINT32) Product code
                         * (UINT32) Version byte
                         * (UINT32) Unknown (0) 
                         */

                        if (Buffer.Length != 16)
                            throw new GameProtocolViolationException(context.Client, "SID_STARTVERSIONING buffer must be 16 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        context.Client.GameState.Platform = (Platform.PlatformCode)r.ReadUInt32();
                        context.Client.GameState.Product = (Product.ProductCode)r.ReadUInt32();
                        context.Client.GameState.Version.VersionByte = r.ReadUInt32();

                        var unknown0 = r.ReadUInt32();

                        if (unknown0 != 0)
                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, string.Format("[" + Common.DirectionToString(context.Direction) + "] SID_STARTVERSIONING unknown field is non-zero (0x{0:X8})", unknown0));

                        return new SID_STARTVERSIONING().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (FILETIME) CheckRevision MPQ filetime
                         *   (STRING) CheckRevision MPQ filename
                         *   (BYTE[]) CheckRevision Formula
                         *     (BYTE) 0
                         */

                        ulong MPQFiletime = 0;
                        string MPQFilename = "ver-IX86-1.mpq";
                        byte[] Formula = Encoding.UTF8.GetBytes("A=3845581634 B=880823580 C=1363937103 4 A=A-S B=B-C C=C-A A=A-B");

                        var fileinfo = new BNFTP.File(MPQFilename).GetFileInfo();
                        if (fileinfo == null)
                        {
                            Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Client_Game, $"Version check file [{MPQFilename}] does not exist!");
                        }
                        else
                        {
                            MPQFilename = fileinfo.Name;
                            MPQFiletime = (ulong)fileinfo.LastWriteTimeUtc.ToFileTimeUtc();
                        }

                        Buffer = new byte[10 + Encoding.UTF8.GetByteCount(MPQFilename) + Formula.Length];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt64)MPQFiletime);
                        w.Write((string)MPQFilename);
                        w.Write(Formula);
                        w.Write((byte)0);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_STARTVERSIONING ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}