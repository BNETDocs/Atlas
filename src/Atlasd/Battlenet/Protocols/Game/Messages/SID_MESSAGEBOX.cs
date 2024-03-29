﻿using Atlasd.Battlenet.Exceptions;
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
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client");

            if (Buffer.Length < 6)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 6 bytes");

            var style = (UInt32)context.Arguments["style"];
            var text = (string)context.Arguments["text"];
            var caption = (string)context.Arguments["caption"];

            Buffer = new byte[6 + Encoding.UTF8.GetByteCount(text) + Encoding.UTF8.GetByteCount(caption)];

            using var m = new MemoryStream(Buffer);
            using var w = new BinaryWriter(m);

            w.Write((UInt32)style);
            w.Write((string)text);
            w.Write((string)caption);

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Sent MessageBox parameters to client");
            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }

        public new byte[] ToByteArray(ProtocolType protocolType)
        {
            if (protocolType.IsChat())
            {
                using var _m = new MemoryStream(Buffer);
                using var r = new BinaryReader(_m);
                var style = r.ReadUInt32();
                var text = r.ReadByteString();
                var caption = r.ReadByteString();

                using var m = new MemoryStream();
                using var w = new System.IO.BinaryWriter(m);
                w.Write(Encoding.UTF8.GetBytes($"{2000 + Id} MESSAGEBOX \""));
                w.Write(text);
                w.Write((byte)'"');
                w.Write(Encoding.UTF8.GetBytes(Battlenet.Common.NewLine));
                return m.GetBuffer()[0..(int)w.BaseStream.Length];
            }
            else
            {
                return base.ToByteArray(protocolType);
            }
        }
    }
}
