﻿using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CREATEACCOUNT2 : Message
    {
        public SID_CREATEACCOUNT2()
        {
            Id = (byte)MessageIds.SID_CREATEACCOUNT2;
            Buffer = new byte[0];
        }

        public SID_CREATEACCOUNT2(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CREATEACCOUNT2;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CREATEACCOUNT2 ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 21)
                            throw new GameProtocolViolationException(context.Client, "SID_CREATEACCOUNT2 buffer must be at least 21 bytes");

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

                        Account.CreateStatus status = Account.TryCreate(username, passwordHash, out var _);

                        return new SID_CREATEACCOUNT2().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {
                            { "status", status } // info key optional
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Status
                         * (STRING) Account name suggestion
                         */

                        string info = context.Arguments.ContainsKey("info") ? (string)context.Arguments["info"] : "";

                        Buffer = new byte[5 + info.Length];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)(Account.CreateStatus)context.Arguments["status"]);
                        w.Write(info);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CREATEACCOUNT2 ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
