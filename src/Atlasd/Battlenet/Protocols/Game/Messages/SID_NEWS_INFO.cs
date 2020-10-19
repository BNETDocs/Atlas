using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
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
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_NEWS_INFO (" + (4 + Buffer.Length) + " bytes)");

                        /**
                         * (UINT32) News timestamp
                         */

                        if (Buffer.Length != 4)
                            throw new GameProtocolViolationException(context.Client, "SID_GETFILETIME buffer must be 4 bytes");

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        var timestamp = r.ReadUInt32();

                        r.Close();
                        m.Close();

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

                        var newsStr = "";
                        var newsGreeting = Channel.GetServerStats(context.Client);
                        var newsTimestamp = DateTime.Now;

                        foreach (var chatEvent in newsGreeting)
                            newsStr += chatEvent.Text + "\r\n";

                        Buffer = new byte[18 + Encoding.UTF8.GetByteCount(newsStr)];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((byte)1);
                        w.Write((UInt32)(lastLogon.ToFileTimeUtc() >> 32));
                        w.Write((UInt32)(newsTimestamp.ToFileTimeUtc() >> 32));
                        w.Write((UInt32)(newsTimestamp.ToFileTimeUtc() >> 32));
                        w.Write((UInt32)(newsTimestamp.ToFileTimeUtc() >> 32));
                        w.Write((string)newsStr);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_NEWS_INFO (" + (4 + Buffer.Length) + " bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
