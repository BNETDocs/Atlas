using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_QUERYADURL : Message
    {
        public SID_QUERYADURL()
        {
            Id = (byte)MessageIds.SID_QUERYADURL;
            Buffer = new byte[0];
        }

        public SID_QUERYADURL(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_QUERYADURL;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        /**
                         * (UINT32) Ad Id
                         */

                        if (Buffer.Length != 4)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var adId = r.ReadUInt32();

                        if (!Battlenet.Common.ActiveAds.TryGetValue(adId, out var ad) || ad == null)
                        {
                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Received url query request for out of bounds ad id [0x{adId:X8}]");
                            return false;
                        }

                        return new SID_QUERYADURL().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){
                            { "adId", adId }, { "adUrl", ad.Url }
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        var adId = (UInt32)context.Arguments["adId"];
                        var adUrl = (string)context.Arguments["adUrl"];

                        /**
                         * (UINT32) Ad Id
                         * (STRING) Ad Url
                         */

                        Buffer = new byte[5 + Encoding.UTF8.GetByteCount(adUrl)];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)adId);
                        w.Write((string)adUrl);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
