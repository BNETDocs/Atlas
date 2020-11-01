using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_GETICONDATA : Message
    {
        public SID_GETICONDATA()
        {
            Id = (byte)MessageIds.SID_GETICONDATA;
            Buffer = new byte[16];
        }

        public SID_GETICONDATA(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_GETICONDATA;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_GETICONDATA ({4 + Buffer.Length} bytes)");

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        if (Buffer.Length != 0)
                            throw new GameProtocolViolationException(context.Client, "SID_GETICONDATA buffer must be 0 bytes");

                        return new SID_GETICONDATA().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (FILETIME) Filetime
                         *   (STRING) Filename
                         */

                        var Filetime = (UInt64)0;
                        var Filename = "icons.bni";

                        Buffer = new byte[9 + Encoding.ASCII.GetByteCount(Filename)];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt64)Filetime);
                        w.Write((string)Filename);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_GETICONDATA ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}