using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_NEWS_INFO : Message
    {
        public SID_NEWS_INFO()
        {
            Id = (byte)MessageIds.SID_NEWS_INFO;
            Buffer = new byte[0];
        }

        public SID_NEWS_INFO(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_NEWS_INFO;
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
                         * (UINT32) News timestamp
                         */

                        if (Buffer.Length != 4)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var timestamp = r.ReadUInt32();

                        return new SID_NEWS_INFO().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "timestamp", timestamp }}));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         *  (UINT8) Number of entries
                         * (UINT32) Last logon timestamp
                         * (UINT32) Oldest news timestamp
                         * (UINT32) Newest news timestamp
                         *
                         * For each entry:
                         *   (UINT32) Timestamp
                         *   (STRING) News
                         */

                        var account = context.Client.GameState.ActiveAccount;
                        var lastLogon = (DateTime)account.Get(Account.LastLogonKey, DateTime.Now);

                        var newsGreeting = Battlenet.Common.GetServerGreeting(context.Client);
                        var newsTimestamp = DateTime.Now;

                        Buffer = new byte[18 + Encoding.UTF8.GetByteCount(newsGreeting)];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((byte)1);
                        w.Write((UInt32)(lastLogon.ToFileTimeUtc() >> 32));
                        w.Write((UInt32)(newsTimestamp.ToFileTimeUtc() >> 32));
                        w.Write((UInt32)(newsTimestamp.ToFileTimeUtc() >> 32));
                        w.Write((UInt32)(newsTimestamp.ToFileTimeUtc() >> 32));
                        w.Write((string)newsGreeting);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
