﻿using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Atlasd.Battlenet
{
    class Account
    {
        public const string AccountCreatedKey = "System\\Account Created";
        public const string ClosedKey = "System\\Closed";
        public const string EmailKey = "System\\Email";
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
        public const string ProfileHomepageKey = "profile\\homepage";
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

        [Flags]
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

        public List<AccountKeyValue> Userdata { get; protected set; }

        private Account()
        {
            Userdata = new List<AccountKeyValue>()
            {
                { new AccountKeyValue(AccountCreatedKey, DateTime.Now, AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(ClosedKey, new byte[0], AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(EmailKey, new byte[0], AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(FailedLogonsKey, (long)0, AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(FlagsKey, Flags.None, AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(FriendsKey, new List<byte[]>(), AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(IPAddressKey, IPAddress.Any, AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(LastLogoffKey, DateTime.Now, AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(LastLogonKey, DateTime.Now, AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(PortKey, 0, AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(PasswordKey, new byte[0], AccountKeyValue.ReadLevel.Internal, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(ProfileAgeKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Owner) },
                { new AccountKeyValue(ProfileDescriptionKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Owner) },
                { new AccountKeyValue(ProfileHomepageKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Owner) },
                { new AccountKeyValue(ProfileLocationKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Owner) },
                { new AccountKeyValue(ProfileSexKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Owner) },
                { new AccountKeyValue(TimeLoggedKey, (long)0, AccountKeyValue.ReadLevel.Owner, AccountKeyValue.WriteLevel.Internal) },
                { new AccountKeyValue(UsernameKey, "", AccountKeyValue.ReadLevel.Any, AccountKeyValue.WriteLevel.Internal) },
            };
        }

        public bool ContainsKey(string key)
        {
            var keyL = key.ToLower();

            lock (Userdata)
            {
                foreach (var kv in Userdata)
                {
                    if (kv.Key.ToLower() == keyL) return true;
                }
            }

            return false;
        }

        public bool Get(byte[] key, out AccountKeyValue value)
        {
            return Get(Encoding.UTF8.GetString(key), out value);
        }

        public bool Get(string key, out AccountKeyValue value)
        {
            lock (Userdata)
            {
                foreach (var kv in Userdata)
                {
                    if (StringComparer.OrdinalIgnoreCase.Compare(kv.Key, key) == 0)
                    {
                        value = kv;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        public dynamic Get(string key, dynamic onKeyNotFound = null)
        {
            if (!Get(key, out AccountKeyValue value) || value == null) return onKeyNotFound;
            return value.Value;
        }

        public void Set(AccountKeyValue value)
        {
            if (value == null) return;

            lock (Userdata)
            {
                foreach (var kv in Userdata)
                {
                    if (StringComparer.OrdinalIgnoreCase.Compare(kv.Key, value.Key) == 0)
                    {
                        Userdata.Remove(kv);
                        break;
                    }
                }

                Userdata.Add(value);
            }
        }

        public void Set(string key, dynamic value)
        {
            lock (Userdata)
            {
                foreach (var kv in Userdata)
                {
                    if (StringComparer.OrdinalIgnoreCase.Compare(kv.Key, key) == 0)
                    {
                        kv.Value = value;
                        return;
                    }
                }
            }

            throw new KeyNotFoundException(key);
        }

        public static CreateStatus TryCreate(string username, byte[] passwordHash, out Account account)
        {
            account = null;

            Settings.State.RootElement.TryGetProperty("account", out var accountJson);
            accountJson.TryGetProperty("auto_admin", out var autoAdminJson);
            accountJson.TryGetProperty("auto_admin_flags", out var autoAdminFlagsJson);
            accountJson.TryGetProperty("disallow_words", out var disallowWordsJson);
            accountJson.TryGetProperty("max_adjacent_punctuation", out var maxAdjacentPunctuationJson);
            accountJson.TryGetProperty("max_length", out var maxLengthJson);
            accountJson.TryGetProperty("max_punctuation", out var maxPunctuationJson);
            accountJson.TryGetProperty("min_alphanumeric", out var minAlphanumericJson);
            accountJson.TryGetProperty("min_length", out var minLengthJson);

            if (!disallowWordsJson.ValueKind.HasFlag(JsonValueKind.Array))
            {
                throw new NotSupportedException("Setting [account] -> [disallow_words] is not an array; check value");
            }

            bool autoAdmin = false;
            if (autoAdminJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var nameJson in autoAdminJson.EnumerateArray())
                {
                    var name = nameJson.GetString();
                    if (username.ToLower() == name.ToLower())
                    {
                        autoAdmin = true;
                        break;
                    }
                }
            }
            else if (autoAdminJson.ValueKind == JsonValueKind.String)
            {
                autoAdmin = username.ToLower() == autoAdminJson.GetString().ToLower();
            }
            else if (autoAdminJson.ValueKind == JsonValueKind.True || autoAdminJson.ValueKind == JsonValueKind.False)
            {
                autoAdmin = autoAdminJson.GetBoolean() && Common.AccountsDb.Count == 0;
            }
            else
            {
                throw new NotSupportedException("Setting [account] -> [auto_admin] is not an array, string, or boolean; check value");
            }

            var autoAdminFlags = (Account.Flags)autoAdminFlagsJson.GetUInt32();
            var bannedWords = disallowWordsJson;
            var maximumAdjacentPunctuation = maxAdjacentPunctuationJson.GetUInt32();
            var maximumPunctuation = maxPunctuationJson.GetUInt32();
            var maximumUsernameSize = maxLengthJson.GetUInt32();
            var minimumAlphanumericSize = minAlphanumericJson.GetUInt32();
            var minimumUsernameSize = minLengthJson.GetUInt32();

            if (Common.AccountsProcessing.ContainsKey(username))
            {
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, "Still processing new account request...");
                return CreateStatus.LastCreateInProgress;
            }
            else if (!Common.AccountsProcessing.TryAdd(username, account))
            {
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Failed to add username [{username}] to accounts processing cache");
                return CreateStatus.LastCreateInProgress;
            }

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, "Processing new account request...");

            if (username.Length < minimumUsernameSize)
                return CreateStatus.UsernameTooShort;

            if (username.Length > maximumUsernameSize)
                return CreateStatus.UsernameShortAlphanumeric;

            uint total_alphanumeric = 0;
            uint total_punctuation = 0;
            uint adjacent_punctuation = 0;
            char last_c = (char)0;

            foreach (var c in username)
            {
                if (!Alphanumeric.Contains(c) && !Punctuation.Contains(c))
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, "Requested username contains invalid characters");
                    return CreateStatus.UsernameInvalidChars;
                }

                if (Alphanumeric.Contains(c))
                    total_alphanumeric++;

                if (Punctuation.Contains(c))
                {
                    total_punctuation++;

                    if (last_c != 0 && Punctuation.Contains(last_c))
                        adjacent_punctuation++;
                }

                if (total_punctuation > maximumPunctuation)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{username}] contains too many punctuation");
                    return CreateStatus.UsernameTooManyPunctuation;
                }

                if (adjacent_punctuation > maximumAdjacentPunctuation)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{username}] contains too many adjacent punctuation");
                    return CreateStatus.UsernameAdjacentPunctuation;
                }

                last_c = c;
            }

            if (total_alphanumeric < minimumAlphanumericSize)
            {
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{username}] is too short or contains too few alphanumeric characters");
                return CreateStatus.UsernameShortAlphanumeric;
            }

            foreach (var word in bannedWords.EnumerateArray())
            {
                if (username.ToLower().Contains(word.GetString().ToLower()))
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{username}] contains a banned word or phrase");
                    return CreateStatus.UsernameBannedWord;
                }
            }

            if (Common.AccountsDb.ContainsKey(username))
            {
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Requested username [{username}] already exists");
                return CreateStatus.AccountExists;
            }

            account = new Account();

            account.Set(Account.UsernameKey, username);
            account.Set(Account.PasswordKey, passwordHash);
            account.Set(Account.FlagsKey, autoAdmin ? autoAdminFlags : Account.Flags.None);

            if (!Common.AccountsDb.TryAdd(username, account))
            {
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Failed to add username [{username}] to accounts database; username already exists");
                return CreateStatus.AccountExists;
            }

            if (!Common.AccountsProcessing.TryRemove(username, out _))
            {
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Failed to remove username [{username}] from accounts processing cache");
                return CreateStatus.LastCreateInProgress;
            }

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Account, $"Created new account [{username}]");
            return CreateStatus.Success;
        }
    }
}
