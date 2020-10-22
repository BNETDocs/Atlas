using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CHECKAD : Message
    {
        public SID_CHECKAD()
        {
            Id = (byte)MessageIds.SID_CHECKAD;
            Buffer = new byte[0];
        }

        public SID_CHECKAD(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CHECKAD;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CHECKAD ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length != 16)
                            throw new GameProtocolViolationException(context.Client, "SID_CHECKAD buffer must be 16 bytes");

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        var platformID            = r.ReadUInt32();
                        var productID             = r.ReadUInt32();
                        var lastDisplayedBannerID = r.ReadUInt32();
                        var currentTime           = r.ReadUInt32();

                        r.Close();
                        m.Close();

                        return true;
                    }
                case MessageDirection.ServerToClient:
                    {
                        var adId          = (UInt32)0;
                        var fileExtension = (UInt32)0;
                        var filetime      = (UInt64)0;
                        var filename      = "";
                        var linkUrl       = "";

                        Buffer = new byte[18];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write(adId);
                        w.Write(fileExtension);
                        w.Write(filetime);
                        w.Write(filename);
                        w.Write(linkUrl);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CHECKAD ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
