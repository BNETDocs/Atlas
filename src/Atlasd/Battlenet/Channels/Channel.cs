using Atlasd.Battlenet.Channels;
using Atlasd.Daemon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Channels
{
    class Channel
    {
        protected static IList<IChannel> Channels = new List<IChannel>();

        public static int DefaultMaxMemberCount() => Settings.GetInt32(new string[]{ "channel", "max_users" }, 40);

        public static bool FindChannel(byte[] targetName, bool autoCreate, out IChannel channel)
        {
            var searchName = targetName;
            if (searchName[0] == '#') searchName = searchName[1..];

            lock (Channels)
            {
                foreach (var ch in Channels)
                {
                    var chName = ch.GetName();
                    if (chName.Length != searchName.Length) continue;

                    bool match = true;
                    for (int i = 0; i < searchName.Length; i++)
                    {
                        if (char.ToLowerInvariant((char)searchName[i]) != char.ToLowerInvariant((char)chName[i]))
                        {
                            match = false;
                            break;
                        }
                    }

                    if (!match) continue;

                    channel = ch;
                    return true;
                }

                if (autoCreate)
                {
                    channel = new EphemeralChannel(searchName);
                    Channels.Add(channel);
                    return true;
                }
                else
                {
                    channel = null;
                    return false;
                }
            }
        }

        public static bool RemoveChannel(IChannel channel)
        {
            lock (Channels) return Channels.Remove(channel);
        }
    }
}
