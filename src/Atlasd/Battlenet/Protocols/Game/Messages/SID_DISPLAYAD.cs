using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_DISPLAYAD : Message
    {
        public SID_DISPLAYAD()
        {
            Id = (byte)MessageIds.SID_DISPLAYAD;
            Buffer = new byte[0];
        }

        public SID_DISPLAYAD(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_DISPLAYAD;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            if (Buffer.Length < 14)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 14 bytes");

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var platformId = r.ReadUInt32();
            var productId = r.ReadUInt32();
            var adId = r.ReadUInt32();
            var adFilename = r.ReadByteString();
            var adUrl = r.ReadByteString();

            if (Battlenet.Common.ActiveAds.TryGetValue(adId, out var ad) && ad != null)
            {
                ad.IncrementDisplayCount();

                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Client displayed advertisement (id: {adId}) (filename: {ad.Filename})");
            }
            else
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Client displayed advertisement (id: 0x{adId:X8}) but id cannot be found");
            }

            return true;
        }
    }
}
