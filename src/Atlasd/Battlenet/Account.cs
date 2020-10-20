using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet
{
    class Account
    {
        public const string AccountCreatedKey = "System\\Account Created";
        public const string FailedLogonsKey = "System\\Total Failed Logons";
        public const string FlagsKey = "System\\Flags";
        public const string FriendsKey = "System\\Friends";
        public const string IPAddressKey = "System\\IP";
        public const string LastLogoffKey = "System\\Last Logoff";
        public const string LastLogonKey = "System\\Last Logon";
        public const string PasswordKey = "System\\Password Digest";
        public const string PortKey = "System\\Port";
        public const string ProfileAgeKey = "profile\\age";
        public const string ProfileDescriptionKey = "profile\\description";
        public const string ProfileLocationKey = "profile\\location";
        public const string ProfileSexKey = "profile\\sex";
        public const string TimeLoggedKey = "System\\Time Logged";
        public const string UsernameKey = "System\\Username";

        public const string Alphanumeric = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string Punctuation = "`~!$%^&*()-_=+[{]}\\|;:'\",<.>/?";

        public enum CreateStatus : UInt32
        {
            Success = 0,
            UsernameTooShort = 1,
            UsernameInvalidChars = 2,
            UsernameBannedWord = 3,
            AccountExists = 4,
            LastCreateInProgress = 5,
            UsernameShortAlphanumeric = 6,
            UsernameAdjacentPunctuation = 7,
            UsernameTooManyPunctuation = 8,
        };

        public enum Flags : UInt32
        {
            None = 0x00,
            Employee = 0x01,
            ChannelOp = 0x02,
            Speaker = 0x04,
            Admin = 0x08,
            NoUDP = 0x10,
            Squelched = 0x20,
            Guest = 0x40,
            Closed = 0x80,
        };

        public Dictionary<string, dynamic> Userdata { get; protected set; }

        private Account()
        {
            Userdata = new Dictionary<string, dynamic>()
            {
                { AccountCreatedKey, (long)DateTime.UtcNow.ToBinary() },
                { FailedLogonsKey, (long)0 },
                { FlagsKey, Flags.None },
                { FriendsKey, new List<string>() },
                { PasswordKey, new byte[0] },
                { ProfileAgeKey, "" },
                { ProfileDescriptionKey, "" },
                { ProfileLocationKey, "" },
                { ProfileSexKey, "" },
                { TimeLoggedKey, (long)0 },
                { UsernameKey, "" },
            };
        }

        public bool ContainsKey(string key)
        {
            return Userdata.ContainsKey(key);
        }

        public dynamic Get(string key, dynamic defaultValue = null)
        {
            if (!Userdata.TryGetValue(key, out dynamic value))
                return defaultValue;

            return value ?? defaultValue;
        }

        public void Set(string key, dynamic value)
        {
            Userdata[key] = value;
        }

        public static CreateStatus TryCreate(string username, byte[] passwordHash, out Account account)
        {
            account = null;

            var BannedWords = (List<string>)Daemon.Common.Settings["account.disallow_words"];
            var MaximumAdjacentPunctuation = (uint)Daemon.Common.Settings["account.max_adjacent_punctuation"];
            var MaximumPunctuation = (uint)Daemon.Common.Settings["account.max_punctuation"];
            var MaximumUsernameSize = (uint)Daemon.Common.Settings["account.max_length"];
            var MinimumAlphanumericSize = (uint)Daemon.Common.Settings["account.min_alphanumeric"];
            var MinimumUsernameSize = (uint)Daemon.Common.Settings["account.min_length"];

            if (username.Length < MinimumUsernameSize)
                return CreateStatus.UsernameTooShort;

            if (username.Length > MaximumUsernameSize)
                return CreateStatus.UsernameShortAlphanumeric;

            uint total_alphanumeric = 0;
            uint total_punctuation = 0;
            uint adjacent_punctuation = 0;
            char last_c = (char)0;

            foreach (var c in username)
            {
                if (!Alphanumeric.Contains(c) && !Punctuation.Contains(c))
                    return CreateStatus.UsernameInvalidChars;

                if (Alphanumeric.Contains(c))
                    total_alphanumeric++;

                if (Punctuation.Contains(c))
                {
                    total_punctuation++;

                    if (last_c != 0 && Punctuation.Contains(last_c))
                        adjacent_punctuation++;
                }

                if (total_punctuation > MaximumPunctuation)
                    return CreateStatus.UsernameTooManyPunctuation;

                if (adjacent_punctuation > MaximumAdjacentPunctuation)
                    return CreateStatus.UsernameAdjacentPunctuation;

                last_c = c;
            }

            if (total_alphanumeric < MinimumAlphanumericSize)
                return CreateStatus.UsernameShortAlphanumeric;

            foreach (var word in BannedWords)
            {
                if (username.Contains(word))
                {
                    return CreateStatus.UsernameBannedWord;
                }
            }

            lock (Common.AccountsDb)
            {
                if (Common.AccountsDb.ContainsKey(username))
                    return CreateStatus.AccountExists;

                account = new Account();

                account.Set(Account.UsernameKey, username);
                account.Set(Account.PasswordKey, passwordHash);

                account.Set(Account.FlagsKey, Common.AccountsDb.Count == 0 ? (Account.Flags.Employee | Account.Flags.Admin) : Account.Flags.None);

                Common.AccountsDb.Add(username, account);
            }

            return CreateStatus.Success;
        }
    }
}
