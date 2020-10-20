using System.Collections.Generic;

namespace Atlasd.Battlenet
{
    class AccountCreationOptions
    {
        public const string Alphanumeric = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string Punctuation = "`~!$%^&*()-_=+[{]}\\|;:'\",<.>/?";

        public List<string> BannedWords = new List<string> {
            "ass",
            "chink",
            "cracker",
            "cunt",
            "fuck",
            "idiot",
            "nigga",
            "nigger",
            "niglet",
            "twat",
            "wetback",
        };

        public static uint MaximumAdjacentPunctuation = 0;
        public static uint MaximumPunctuation = 7;
        public static uint MaximumUsernameSize = 15;
        public static uint MinimumAlphanumericSize = 1;
        public static uint MinimumUsernameSize = 3;
    }
}
