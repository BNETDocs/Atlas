using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CLANMEMBERLIST : Message
    {
        public SID_CLANMEMBERLIST()
        {
            Id = (byte)MessageIds.SID_CLANMEMBERLIST;
            Buffer = new byte[0];
        }

        public SID_CLANMEMBERLIST(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CLANMEMBERLIST;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (context.Client.GameState.ActiveAccount == null)
                        {
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} received before logon");
                        }

                        /**
                         * (UINT32) Cookie
                         */

                        if (Buffer.Length != 4)
                        {
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes");
                        }

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var cookie = r.ReadUInt32();

                        return new SID_CLANMEMBERLIST().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){
                            { "cookie", cookie },
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        var cookie = (UInt32)context.Arguments["cookie"];

                        /**
                         * (UINT32) Cookie
                         *  (UINT8) Member Count
                         *
                         * For each member:
                         *   (STRING) Username
                         *    (UINT8) Rank
                         *    (UINT8) Online Status
                         *   (STRING) Location
                         */

                        Buffer = new byte[5];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write(cookie);
                        w.Write((byte)0);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
