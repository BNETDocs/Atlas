using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_NOTIFYJOIN : Message
    {

        public SID_NOTIFYJOIN()
        {
            Id = (byte)MessageIds.SID_NOTIFYJOIN;
            Buffer = new byte[0];
        }

        public SID_NOTIFYJOIN(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_NOTIFYJOIN;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_NOTIFYJOIN ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, "SID_NOTIFYJOIN must be sent from client to server");

            if (Buffer.Length < 10)
                throw new GameProtocolViolationException(context.Client, "SID_NOTIFYJOIN buffer must be at least 10 bytes");

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var productId = r.ReadUInt32();
            var productVersion = r.ReadUInt32();
            var gameName = r.ReadString();
            var gamePassword = r.ReadString();

            try
            {
                lock (context.Client.GameState.ActiveChannel)
                    context.Client.GameState.ActiveChannel.RemoveUser(context.Client.GameState);
            }
            catch (ArgumentNullException) { }
            catch (NullReferenceException) { }

            return true;
        }
    }
}
