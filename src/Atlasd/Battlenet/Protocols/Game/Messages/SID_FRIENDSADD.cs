using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_FRIENDSADD : Message
    {

        public SID_FRIENDSADD()
        {
            Id = (byte)MessageIds.SID_FRIENDSADD;
            Buffer = new byte[0];
        }

        public SID_FRIENDSADD(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_FRIENDSADD;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction != MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client");

            /**
             * (STRING) Account
             *  (UINT8) Status
             *  (UINT8) Location id
             * (UINT32) Product id
             * (STRING) Location name
             */

            var friend = (Friend)context.Arguments["friend"];
            var account = (byte[])friend.Username;
            var status = (byte)friend.StatusId;
            var location = (byte)friend.LocationId;
            var product = (UInt32)friend.ProductCode;
            var locationStr = (byte[])friend.LocationString;

            var bufferSize = (uint)(8 + account.Length + locationStr.Length);

            Buffer = new byte[bufferSize];

            using var m = new MemoryStream(Buffer);
            using var w = new BinaryWriter(m);

            w.WriteByteString(account);
            w.Write(status);
            w.Write(location);
            w.Write(product);
            w.WriteByteString(locationStr);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
