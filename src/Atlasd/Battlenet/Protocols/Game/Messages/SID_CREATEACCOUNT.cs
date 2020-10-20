using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CREATEACCOUNT : Message
    {
        protected enum Statuses : UInt32
        {
            Failure = 0,
            Success = 1,
        };

        public SID_CREATEACCOUNT()
        {
            Id = (byte)MessageIds.SID_CREATEACCOUNT;
            Buffer = new byte[0];
        }

        public SID_CREATEACCOUNT(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CREATEACCOUNT;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CREATEACCOUNT ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 21)
                            throw new GameProtocolViolationException(context.Client, "SID_CREATEACCOUNT buffer must be at least 21 bytes");

                        /**
                         * (UINT32) [5] Password hash
                         * (STRING) Username
                         */

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        var passwordHash = r.ReadBytes(20);
                        var username = r.ReadString();

                        r.Close();
                        m.Close();

                        Account.CreateStatus _status = Account.TryCreate(username, passwordHash, out var _);
                        var status = _status == Account.CreateStatus.Success ? Statuses.Success : Statuses.Failure;

                        return new SID_CREATEACCOUNT().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {
                            { "status", status }
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Status
                         */

                        Buffer = new byte[4];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)(Statuses)context.Arguments["status"]);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CREATEACCOUNT ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
