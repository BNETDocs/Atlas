using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_AUTH_ACCOUNTLOGONPROOF : Message
    {
        public enum Statuses : UInt32
        {
            Success = 0x00,
            BadPassword = 0x02,
            UsernameTooShort = 0x07,
            UsernameInvalidChars = 0x08,
            UsernameBannedWord = 0x09,
            UsernameShortAlphanumeric = 0x0A,
            UsernameAdjacentPunctuation = 0x0B,
            UsernameTooManyPunctuation = 0x0C,
        }

        public SID_AUTH_ACCOUNTLOGONPROOF()
        {
            Id = (byte)MessageIds.SID_AUTH_ACCOUNTLOGONPROOF;
            Buffer = new byte[0];
        }

        public SID_AUTH_ACCOUNTLOGONPROOF(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_AUTH_ACCOUNTLOGONPROOF;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context == null || context.Client == null || !context.Client.Connected || context.Client.GameState == null) return false;

            if (context.Direction != MessageDirection.ServerToClient)
                throw new Exceptions.GameProtocolViolationException(context.Client, $"{MessageName(Id)} is not supported from clients");

            /**
             * (UINT32)    Status
             * (UINT8)[20] Server password proof
             */

            var status = (Statuses)context.Arguments["status"];

            Buffer = new byte[24];

            using var m = new MemoryStream(Buffer);
            using var w = new BinaryWriter(m);

            w.Write((UInt32)status);
            w.Write(new byte[20]);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes) (status: 0x{(UInt32)status:X8})");
            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
