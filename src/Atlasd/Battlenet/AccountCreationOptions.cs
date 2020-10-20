using System.Collections.Generic;

namespace Atlasd.Battlenet
{
    class AccountCreationOptions
    {
        public const string Alphanumeric = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string Punctuation = "`~!$%^&*()-_=+[{]}\\|;:'\",<.>/?";

        public static List<string> BannedWords = (List<string>)Daemon.Common.Settings["account.disallow_words"];

        public static uint MaximumAdjacentPunctuation = 0;
        public static uint MaximumPunctuation = 7;
        public static uint MaximumUsernameSize = 15;
        public static uint MinimumAlphanumericSize = 1;
        public static uint MinimumUsernameSize = 3;
    }
}
