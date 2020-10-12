using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CREATEACCOUNT2 : Message
    {
        protected enum Statuses : UInt32
        {
            None = 0x7fffffff,
            AccountCreated = 0,
            UsernameTooShort = 1,
            UsernameInvalidChars = 2,
            UsernameBannedWord = 3,
            AccountExists = 4,
            LastCreateInProgress = 5,
            UsernameShortAlphanumeric = 6,
            UsernameAdjacentPunctuation = 7,
            UsernameTooManyPunctuation = 8,
        };

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
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_CREATEACCOUNT2 (" + (4 + Buffer.Length) + " bytes)");

                        if (Buffer.Length < 5)
                            throw new ProtocolViolationException(context.Client.ProtocolType, "SID_CREATEACCOUNT2 buffer must be at least 5 bytes");

                        /**
                         * (UINT32) [5] Password hash
                         * (STRING) Username
                         */

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        var passwordHash = r.ReadBytes(20);
                        var username = r.ReadString();

                        Statuses status = Statuses.None;

                        if (status == Statuses.None && username.Length < AccountCreationOptions.MinimumUsernameSize)
                            status = Statuses.UsernameTooShort;

                        if (status == Statuses.None && username.Length > AccountCreationOptions.MaximumUsernameSize)
                            status = Statuses.UsernameShortAlphanumeric;

                        uint total_alphanumeric = 0;
                        uint total_punctuation = 0;
                        uint adjacent_punctuation = 0;
                        char last_c = (char)0;

                        foreach (var c in username)
                        {
                            if (AccountCreationOptions.Alphanumeric.Contains(c))
                                total_alphanumeric++;

                            if (AccountCreationOptions.Punctuation.Contains(c))
                            {
                                total_punctuation++;

                                if (last_c != 0 && AccountCreationOptions.Punctuation.Contains(last_c))
                                    adjacent_punctuation++;
                            }

                            if (total_punctuation > AccountCreationOptions.MaximumPunctuation)
                            {
                                status = Statuses.UsernameTooManyPunctuation;
                                break;
                            }

                            if (adjacent_punctuation > AccountCreationOptions.MaximumAdjacentPunctuation)
                            {
                                status = Statuses.UsernameAdjacentPunctuation;
                                break;
                            }

                            last_c = c;
                        }

                        if (status == Statuses.None && total_alphanumeric < AccountCreationOptions.MinimumAlphanumericSize)
                            status = Statuses.UsernameShortAlphanumeric;

                        if (status == Statuses.None && Battlenet.Common.AccountsDb.ContainsKey(username))
                            status = Statuses.AccountExists;

                        if (status == Statuses.None)
                        {
                            var account = new Account();

                            account.Set(Account.UsernameKey, username);
                            account.Set(Account.PasswordKey, passwordHash);

                            account.Set(Account.FlagsKey, Battlenet.Common.AccountsDb.Count == 0 ? (Account.Flags.Employee | Account.Flags.Admin) : (UInt32)0);

                            account.Set(Account.AccountCreatedKey, (long)DateTime.UtcNow.ToBinary());
                            account.Set(Account.LastLogoffKey, null);
                            account.Set(Account.LastLogonKey, null);
                            account.Set(Account.TimeLoggedKey, (uint)0);

                            lock (Battlenet.Common.AccountsDb) Battlenet.Common.AccountsDb.Add(username, account);
                            status = Statuses.AccountCreated;
                        }

                        r.Close();
                        m.Close();

                        return new SID_CREATEACCOUNT2().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {
                            { "status", status }, { "info", (string)"" }
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Status
                         * (STRING) Account name suggestion
                         */

                        Buffer = new byte[5 + ((string)context.Arguments["info"]).Length];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)(Statuses)context.Arguments["status"]);

                        if (context.Arguments.ContainsKey("info"))
                            w.Write((string)context.Arguments["info"]);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_CREATEACCOUNT2 (" + (4 + Buffer.Length) + " bytes)");
                        context.Client.Client.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
