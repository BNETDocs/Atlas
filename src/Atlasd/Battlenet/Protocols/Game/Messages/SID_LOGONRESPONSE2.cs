using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LOGONRESPONSE2 : Message
    {
        protected enum Statuses : UInt32
        {
            None = 0x7fffffff,
            Success = 0,
            AccountNotFound = 1,
            BadPassword = 2,
            AccountClosed = 6,
        };

        public SID_LOGONRESPONSE2()
        {
            Id = (byte)MessageIds.SID_LOGONRESPONSE2;
            Buffer = new byte[0];
        }

        public SID_LOGONRESPONSE2(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_LOGONRESPONSE2;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_LOGONRESPONSE2 (" + (4 + Buffer.Length) + " bytes)");

                        if (Buffer.Length < 29)
                            throw new ProtocolViolationException(context.Client.ProtocolType, "SID_LOGONRESPONSE2 buffer must be at least 29 bytes");

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

                        Account account;
                        Statuses status = Statuses.None;

                        Battlenet.Common.AccountsDb.TryGetValue(context.Client.GameState.Username, out account);

                        if (status == Statuses.None && account == null)
                            status = Statuses.AccountNotFound;

                        if (status == Statuses.None)
                        {
                            var passwordHashDb = (byte[])account.Get(Account.PasswordKey);
                            var compareHash = OldAuth.CheckDoubleHashData(passwordHashDb, clientToken, serverToken);
                            if (compareHash.Equals(passwordHash)) status = Statuses.BadPassword;
                        }

                        if (status == Statuses.None)
                        {
                            var flags = (Account.Flags)account.Get(Account.FlagsKey);
                            if ((flags & Account.Flags.Closed) != 0) status = Statuses.AccountClosed;
                        }
                        
                        if (status == Statuses.None)
                        {
                            context.Client.GameState.ActiveAccount = account;
                            context.Client.GameState.OnlineName = context.Client.GameState.Username;

                            context.Client.GameState.ActiveAccount.Set(Account.IPAddressKey, context.Client.RemoteEndPoint.ToString().Split(":")[0]);
                            context.Client.GameState.ActiveAccount.Set(Account.LastLogonKey, DateTime.Now);
                            context.Client.GameState.ActiveAccount.Set(Account.PortKey, context.Client.RemoteEndPoint.ToString().Split(":")[1]);

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
                         * (STRING) Additional information (optional)
                         */

                        Buffer = new byte[4 + (context.Arguments.ContainsKey("info") ? 1 + ((string)context.Arguments["info"]).Length : 0)];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)(Statuses)context.Arguments["status"]);

                        if (context.Arguments.ContainsKey("info"))
                            w.Write((string)context.Arguments["info"]);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_LOGONRESPONSE2 (" + (4 + Buffer.Length) + " bytes)");
                        context.Client.Client.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
