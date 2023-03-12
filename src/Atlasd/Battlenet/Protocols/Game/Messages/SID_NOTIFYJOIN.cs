using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Linq;
using System.Text;

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
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            if (Buffer.Length < 10)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 10 bytes");

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var productId = r.ReadUInt32();
            var productVersion = r.ReadUInt32();
            var gameName = r.ReadByteString();
            var gamePassword = r.ReadByteString();

            if (context.Client.GameState.ActiveChannel != null)
                context.Client.GameState.ActiveChannel.RemoveUser(context.Client.GameState);

            lock (Battlenet.Common.ActiveGameAds)
            {
                foreach (var gameAd in Battlenet.Common.ActiveGameAds)
                {
                    if (gameAd.Name.SequenceEqual(gameName))
                    {
                        if (gameAd.HasClient(context.Client.GameState) || gameAd.AddClient(context.Client.GameState))
                            context.Client.GameState.GameAd = gameAd;
                        break;
                    }
                }
            }

            return true;
        }
    }
}
