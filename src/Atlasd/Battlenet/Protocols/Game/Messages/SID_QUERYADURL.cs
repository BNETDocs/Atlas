using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_QUERYADURL : Message
    {
        public SID_QUERYADURL()
        {
            Id = (byte)MessageIds.SID_QUERYADURL;
            Buffer = new byte[0];
        }

        public SID_QUERYADURL(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_QUERYADURL;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_QUERYADURL ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length != 4)
                            throw new GameProtocolViolationException(context.Client, "SID_QUERYADURL buffer must be 4 bytes");

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        var adId = r.ReadUInt32();

                        r.Close();
                        m.Close();

                        return true;
                    }
                case MessageDirection.ServerToClient:
                    {
                        var adId = (UInt32)context.Arguments["adId"];
                        var adUrl = (string)context.Arguments["adUrl"];

                        Buffer = new byte[5 + Encoding.ASCII.GetByteCount(adUrl)];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write(adId);
                        w.Write(adUrl);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_QUERYADURL ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
