using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CHECKAD : Message
    {
        public SID_CHECKAD()
        {
            Id = (byte)MessageIds.SID_CHECKAD;
            Buffer = new byte[0];
        }

        public SID_CHECKAD(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CHECKAD;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CHECKAD ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length != 16)
                            throw new GameProtocolViolationException(context.Client, "SID_CHECKAD buffer must be 16 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var platformID    = r.ReadUInt32();
                        var productID     = r.ReadUInt32();
                        var lastShownAdId = r.ReadUInt32();
                        var currentTime   = r.ReadUInt32();

                        return new SID_CHECKAD().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object>(){
                            { "platformId", platformID },
                            { "productId", productID },
                            { "lastShownAdId", lastShownAdId },
                            { "currentTime", currentTime },
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        var rand = new Random();
                        uint adId;
                        Advertisement ad;

                        // Get random advertisement
                        lock (Battlenet.Common.ActiveAds)
                        {
                            adId = (uint)rand.Next(0, Battlenet.Common.ActiveAds.Count - 1);
                            ad = Battlenet.Common.ActiveAds[(int)adId];
                        }

                        Buffer = new byte[18 + Encoding.ASCII.GetByteCount(ad.Filename) + Encoding.ASCII.GetByteCount(ad.Url)];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)adId);
                        w.Write((UInt32)0); // File extension
                        w.Write(ad.Filetime.ToFileTimeUtc());
                        w.Write(ad.Filename);
                        w.Write(ad.Url);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CHECKAD ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
