using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CLANMEMBERSTATUSCHANGE : Message
    {
        public SID_CLANMEMBERSTATUSCHANGE()
        {
            Id = (byte)MessageIds.SID_CLANMEMBERSTATUSCHANGE;
            Buffer = new byte[6];
        }

        public SID_CLANMEMBERSTATUSCHANGE(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CLANMEMBERSTATUSCHANGE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction != MessageDirection.ServerToClient)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client");
            }

            /**
             * (STRING) Username
             *  (UINT8) Rank
             *  (UINT8) Online status
             * (STRING) Location
             */

            byte[] username = (byte[])context.Arguments["username"];
            byte rank = (byte)context.Arguments["rank"];
            byte status = (byte)context.Arguments["status"];
            byte[] location = (byte[])context.Arguments["location"];

            Buffer = new byte[4 + username.Length + location.Length];

            using var m = new MemoryStream(Buffer);
            using var w = new BinaryWriter(m);

            w.WriteByteString(username);
            w.Write(rank);
            w.Write(status);
            w.WriteByteString(location);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
