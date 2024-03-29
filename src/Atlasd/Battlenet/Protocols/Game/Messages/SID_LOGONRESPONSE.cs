﻿using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LOGONRESPONSE : Message
    {
        public enum Statuses : UInt32
        {
            Failure = 0,
            Success = 1,
        };

        public SID_LOGONRESPONSE()
        {
            Id = (byte)MessageIds.SID_LOGONRESPONSE;
            Buffer = new byte[0];
        }

        public SID_LOGONRESPONSE(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_LOGONRESPONSE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context == null || context.Client == null || !context.Client.Connected || context.Client.GameState == null) return false;

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 29)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 29 bytes");

                        if (context.Client.GameState.ActiveAccount != null)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} cannot be sent after logging into an account");

                        /**
                         * (UINT32)     Client Token
                         * (UINT32)     Server Token
                         *  (UINT8)[20] Password Hash
                         * (STRING)     Username
                         */

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var clientToken = r.ReadUInt32();
                        var serverToken = r.ReadUInt32();
                        var passwordHash = r.ReadBytes(20);
                        context.Client.GameState.Username = r.ReadString();

                        if (!Battlenet.Common.AccountsDb.TryGetValue(context.Client.GameState.Username, out Account account) || account == null)
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{context.Client.GameState.Username}] does not exist");
                            return new SID_LOGONRESPONSE().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "status", Statuses.Failure }}));
                        }

                        var passwordHashDb = (byte[])account.Get(Account.PasswordKey, new byte[20]);
                        var compareHash = OldAuth.CheckDoubleHashData(passwordHashDb, clientToken, serverToken);
                        if (!compareHash.SequenceEqual(passwordHash))
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{context.Client.GameState.Username}] logon failed password mismatch");
                            account.Set(Account.FailedLogonsKey, ((UInt32)account.Get(Account.FailedLogonsKey, (UInt32)0)) + 1);
                            return new SID_LOGONRESPONSE().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "status", Statuses.Failure }}));
                        }

                        var flags = (Account.Flags)account.Get(Account.FlagsKey, Account.Flags.None);
                        if ((flags & Account.Flags.Closed) != 0)
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{context.Client.GameState.Username}] logon failed account closed");
                            account.Set(Account.FailedLogonsKey, ((UInt32)account.Get(Account.FailedLogonsKey, (UInt32)0)) + 1);
                            return new SID_LOGONRESPONSE().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "status", Statuses.Failure }}));
                        }

                        context.Client.GameState.ActiveAccount = account;
                        context.Client.GameState.FailedLogons = (UInt32)account.Get(Account.FailedLogonsKey, (UInt32)0);
                        context.Client.GameState.LastLogon = (DateTime)account.Get(Account.LastLogonKey, DateTime.Now);

                        account.Set(Account.FailedLogonsKey, (UInt32)0);
                        account.Set(Account.IPAddressKey, context.Client.RemoteEndPoint.ToString().Split(":")[0]);
                        account.Set(Account.LastLogonKey, DateTime.Now);
                        account.Set(Account.PortKey, context.Client.RemoteEndPoint.ToString().Split(":")[1]);

                        var serial = 1;
                        var onlineName = context.Client.GameState.Username;
                        while (!Battlenet.Common.ActiveAccounts.TryAdd(onlineName, account)) onlineName = $"{context.Client.GameState.Username}#{++serial}";
                        context.Client.GameState.OnlineName = onlineName;

                        context.Client.GameState.Username = (string)account.Get(Account.UsernameKey, context.Client.GameState.Username);

                        if (!Battlenet.Common.ActiveGameStates.TryAdd(context.Client.GameState.OnlineName, context.Client.GameState))
                        {
                            Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Failed to add game state to active game state cache");
                            account.Set(Account.FailedLogonsKey, ((UInt32)account.Get(Account.FailedLogonsKey, (UInt32)0)) + 1);
                            Battlenet.Common.ActiveAccounts.TryRemove(onlineName, out _);
                            return false;
                        }

                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{context.Client.GameState.Username}] logon success as [{context.Client.GameState.OnlineName}]");
                        if (!(new SID_LOGONRESPONSE()).Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "status", Statuses.Success }}))) return false;

                        var emailAddress = account.Get(Account.EmailKey, new byte[0]);
                        if (emailAddress.Length == 0)
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{context.Client.GameState.Username}] does not have an email set");
                            new SID_SETEMAIL().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                        }

                        return true;
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Status
                         */

                        var status = (UInt32)(Statuses)context.Arguments["status"];

                        Buffer = new byte[4];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write(status);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes) (status: 0x{status:X8})");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
