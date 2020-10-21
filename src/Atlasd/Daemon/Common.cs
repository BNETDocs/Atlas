using System;
using System.Collections.Generic;

namespace Atlasd.Daemon
{
    class Common
    {
        public static Dictionary<string, dynamic> Settings { get; private set; }

        public static void Initialize()
        {
            Settings = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase)
            {
                { "account.auto_admin", true },
                {
                    "account.disallow_words",
                    new List<string>()
                    {
                        "ass",
                        "battle.net",
                        "blizzard",
                        "chink",
                        "cracker",
                        "cunt",
                        "fag",
                        "faggot",
                        "fuck",
                        "idiot",
                        "nigga",
                        "nigger",
                        "niglet",
                        "twat",
                        "wetback",
                    }
                },
                { "account.max_adjacent_punctuation", 0 },
                { "account.max_length", 15 },
                { "account.max_punctuation", 7 },
                { "account.min_alphanumeric", 1 },
                { "account.min_length", 3 },
                { "battlenet.listener.interface", "0.0.0.0" },
                { "battlenet.listener.port", 6112 },
                { "channel.auto_op", true },
                { "channel.max_length", 31 },
                { "channel.max_users", 40 },
            };
        }

        public static void Start()
        {
            Battlenet.Common.Listener.Start();
        }
    }
}
