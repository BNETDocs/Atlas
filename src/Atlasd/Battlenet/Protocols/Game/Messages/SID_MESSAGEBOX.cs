using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_MESSAGEBOX : Message
    {
        public SID_MESSAGEBOX()
        {
            Id = (byte)MessageIds.SID_MESSAGEBOX;
            Buffer = new byte[6];
        }

        public SID_MESSAGEBOX(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_MESSAGEBOX;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_MESSAGEBOX ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, "SID_MESSAGEBOX must be sent from server to client");

            if (Buffer.Length < 6)
                throw new GameProtocolViolationException(context.Client, "SID_MESSAGEBOX buffer must be at least 6 bytes");

            var style = (UInt32)context.Arguments["style"];
            var text = (string)context.Arguments["text"];
            var caption = (string)context.Arguments["caption"];

            Buffer = new byte[6 + Encoding.UTF8.GetByteCount(text) + Encoding.UTF8.GetByteCount(caption)];

            using var m = new MemoryStream(Buffer);
            using var w = new BinaryWriter(m);

            w.Write((UInt32)style);
            w.Write(Encoding.UTF8.GetBytes(text));
            w.Write((byte)0);
            w.Write(Encoding.UTF8.GetBytes(caption));
            w.Write((byte)0);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_MESSAGEBOX ({4 + Buffer.Length} bytes)");
            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
