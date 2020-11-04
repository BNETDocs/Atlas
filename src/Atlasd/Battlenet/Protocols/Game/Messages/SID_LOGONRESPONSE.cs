using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LOGONRESPONSE : Message
    {
        protected enum Statuses : UInt32
        {
            None = 255,
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
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_LOGONRESPONSE ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 29)
                            throw new GameProtocolViolationException(context.Client, "SID_LOGONRESPONSE buffer must be at least 29 bytes");

                        /**
                         * (UINT32)     Client Token
                         * (UINT32)     Server Token
                         *  (UINT8)[20] Password Hash
                         * (STRING)     Username
                         */

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        var clientToken = r.ReadUInt32();
                        var serverToken = r.ReadUInt32();
                        var passwordHash = r.ReadBytes(20);
                        context.Client.GameState.Username = r.ReadString();

                        Statuses status = Statuses.None;

                        Battlenet.Common.AccountsDb.TryGetValue(context.Client.GameState.Username, out Account account);

                        if (status == Statuses.None && account == null)
                            status = Statuses.Failure;

                        if (status == Statuses.None)
                        {
                            var passwordHashDb = (byte[])account.Get(Account.PasswordKey, new byte[20]);
                            var compareHash = OldAuth.CheckDoubleHashData(passwordHashDb, clientToken, serverToken);
                            if (!compareHash.SequenceEqual(passwordHash)) status = Statuses.Failure;
                        }

                        if (status == Statuses.None)
                        {
                            var flags = (Account.Flags)account.Get(Account.FlagsKey, Account.Flags.None);
                            if ((flags & Account.Flags.Closed) != 0) status = Statuses.Failure;
                        }
                        
                        if (status == Statuses.None)
                        {
                            context.Client.GameState.ActiveAccount = account;
                            context.Client.GameState.LastLogon = (DateTime)account.Get(Account.LastLogonKey, DateTime.Now);

                            account.Set(Account.IPAddressKey, context.Client.RemoteEndPoint.ToString().Split(":")[0]);
                            account.Set(Account.LastLogonKey, DateTime.Now);
                            account.Set(Account.PortKey, context.Client.RemoteEndPoint.ToString().Split(":")[1]);

                            lock (Battlenet.Common.ActiveAccounts)
                            {
                                var serial = 1;
                                var onlineName = context.Client.GameState.Username;

                                while (Battlenet.Common.ActiveAccounts.ContainsKey(onlineName))
                                {
                                    onlineName = $"{context.Client.GameState.Username}#{++serial}";
                                }

                                context.Client.GameState.OnlineName = onlineName;
                                Battlenet.Common.ActiveAccounts.Add(onlineName, account);
                            }

                            context.Client.GameState.Username = (string)account.Get(Account.UsernameKey, context.Client.GameState.Username);

                            lock (Battlenet.Common.ActiveGameStates)
                            {
                                Battlenet.Common.ActiveGameStates.Add(context.Client.GameState.OnlineName, context.Client.GameState);
                            }

                            status = Statuses.Success;
                        }

                        r.Close();
                        m.Close();

                        return new SID_LOGONRESPONSE2().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {
                            { "status", status },
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

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_LOGONRESPONSE ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
