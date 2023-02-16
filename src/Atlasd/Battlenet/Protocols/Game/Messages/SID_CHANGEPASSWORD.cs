using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CHANGEPASSWORD : Message
    {
        protected enum Statuses : UInt32
        {
            Success = 0,
            Failure = 1,
        };

        public SID_CHANGEPASSWORD()
        {
            Id = (byte)MessageIds.SID_CHANGEPASSWORD;
            Buffer = new byte[0];
        }

        public SID_CHANGEPASSWORD(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CHANGEPASSWORD;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            var gameState = context.Client.GameState;

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 49)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 49 bytes");

                        if (gameState.ActiveAccount != null)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent before logon");

                        /**
                         * (UINT32)     Client Token
                         * (UINT32)     Server Token
                         *  (UINT8)[20] Old Password Hash
                         *  (UINT8)[20] New Password Hash
                         * (STRING)     Username
                         */

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var clientToken = r.ReadUInt32();
                        var serverToken = r.ReadUInt32();
                        var passwordHash1 = r.ReadBytes(20);
                        var passwordHash2 = r.ReadBytes(20);
                        var username = r.ReadString();

                        if (!Battlenet.Common.AccountsDb.TryGetValue(username, out Account account) || account == null)
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{username}] does not exist");
                            return new SID_CHANGEPASSWORD().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "status", Statuses.Failure }}));
                        }

                        var passwordHashDb = (byte[])account.Get(Account.PasswordKey, new byte[20]);
                        var compareHash = OldAuth.CheckDoubleHashData(passwordHashDb, clientToken, serverToken);
                        if (!compareHash.SequenceEqual(passwordHash1))
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{username}] password change failed password mismatch");
                            account.Set(Account.FailedLogonsKey, ((UInt32)account.Get(Account.FailedLogonsKey, (UInt32)0)) + 1);
                            return new SID_CHANGEPASSWORD().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "status", Statuses.Failure }}));
                        }

                        var flags = (Account.Flags)account.Get(Account.FlagsKey, Account.Flags.None);
                        if ((flags & Account.Flags.Closed) != 0)
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{username}] password change failed account closed");
                            account.Set(Account.FailedLogonsKey, ((UInt32)account.Get(Account.FailedLogonsKey, (UInt32)0)) + 1);
                            return new SID_CHANGEPASSWORD().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "status", Statuses.Failure }}));
                        }

                        account.Set(Account.PasswordKey, passwordHash2);

                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{username}] password change success");
                        return new SID_CHANGEPASSWORD().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {{ "status", Statuses.Success }}));
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
