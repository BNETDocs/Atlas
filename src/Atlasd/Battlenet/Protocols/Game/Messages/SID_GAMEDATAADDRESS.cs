using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Net;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_GAMEDATAADDRESS : Message
    {

        public SID_GAMEDATAADDRESS()
        {
            Id = (byte)MessageIds.SID_GAMEDATAADDRESS;
            Buffer = new byte[0];
        }

        public SID_GAMEDATAADDRESS(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_GAMEDATAADDRESS;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            if (Buffer.Length != 16)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 16 bytes");

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var unknown0 = r.ReadUInt16();
            var port     = r.ReadUInt16();
            var address  = r.ReadBytes(4);
            var unknown1 = r.ReadUInt32();
            var unknown2 = r.ReadUInt32();

            context.Client.GameState.GameDataAddress = new IPAddress(address);
            context.Client.GameState.GameDataPort = port;

            context.Client.Send(ToByteArray(context.Client.ProtocolType));
            return true;
        }
    }
}
