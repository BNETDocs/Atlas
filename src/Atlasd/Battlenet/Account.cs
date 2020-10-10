using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet
{
    class Account
    {
        public const string AccountCreatedKey = "System\\Account Created";
        public const string FlagsKey = "System\\Flags";
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

        public enum Flags : UInt32
        {
            Employee = 0x01,
            ChannelOp = 0x02,
            Speaker = 0x04,
            Admin = 0x08,
            Guest = 0x40,
            Closed = 0x80,
        };

        public Dictionary<string, object> Userdata { get; protected set; }

        public Account()
        {
            Userdata = new Dictionary<string, object>();
        }

        public bool ContainsKey(string key)
        {
            return Userdata.ContainsKey(key);
        }

        public object Get(string key)
        {
            return Userdata[key];
        }

        public void Set(string key, object value)
        {
            Userdata[key] = value;
        }
    }
}
