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
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length != 16)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 16 bytes");

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
                        // Get advertisement by random index from ActiveAds, in the future this could be managed via ad campaigns
                        UInt32 adId = (UInt32)(new Random()).Next(0, Battlenet.Common.ActiveAds.Count);
                        if (!Battlenet.Common.ActiveAds.TryGetValue(adId, out var ad) || ad == null)
                        {
                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, $"Failed to get advertisement [0x{adId:X8}] from active advertisement cache");
                            return true;
                        }

                        var extStr = Path.GetExtension(ad.Filename);
                        var fileExtension = new byte[4];
                        if (!string.IsNullOrEmpty(extStr))
                        {
                            extStr = extStr.Substring(0, Math.Min(extStr.Length, 4));
                            Encoding.ASCII.GetBytes(extStr, 0, extStr.Length, fileExtension, 0);
                        }

                        Buffer = new byte[18 + Encoding.UTF8.GetByteCount(ad.Filename) + Encoding.UTF8.GetByteCount(ad.Url)];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)adId);
                        w.Write(fileExtension);
                        w.Write((UInt64)ad.Filetime.ToFileTimeUtc());
                        w.Write((string)ad.Filename);
                        w.Write((string)ad.Url);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
