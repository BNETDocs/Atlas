using Atlasd.Battlenet.Protocols.Game.ChatCommands;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game
{
    class ChatCommand
    {
        public List<string> Arguments { get; protected set; }
        public byte[] RawBuffer { get; protected set; }

        /**
         * <param name="rawBuffer">The byte array from the client, stripped of the slash and null-terminator. Value may be used during invocation of a ChatCommand.</param>
         */
        public ChatCommand(byte[] rawBuffer, List<string> arguments)
        {
            Arguments = arguments;
            RawBuffer = rawBuffer;
        }

        public virtual bool CanInvoke(ChatCommandContext context)
        {
            return false;
        }

        public virtual void Invoke(ChatCommandContext context)
        {
            throw new NotSupportedException("Base ChatCommand class does not Invoke()");
        }

        public static ChatCommand FromByteArray(byte[] text)
        {
            return Parse(Encoding.UTF8.GetString(text), text);
        }

        public static ChatCommand FromString(string text)
        {
            return Parse(text, Encoding.UTF8.GetBytes(text));
        }

        private static ChatCommand Parse(string text, byte[] raw)
        {
            var args = new List<string>(text.Split(' '));

            var cmd = args[0];
            args.RemoveAt(0);

            // Calculates and removes (cmd+' ') from (raw) which prints into (_raw):
            var stripSize = cmd.Length + (text.Length - cmd.Length > 0 ? 1 : 0);
            var _raw = raw[stripSize..];

            switch (cmd.ToLowerInvariant())
            {
                case "admin":
                    return new AdminCommand(_raw, args);
                case "away":
                    return new AwayCommand(_raw, args);
                case "ban":
                    return new BanCommand(_raw, args);
                case "clan":
                    return new ClanCommand(_raw, args);
                case "channel":
                case "join":
                case "j":
                    return new JoinCommand(_raw, args);
                case "designate":
                    return new DesignateCommand(_raw, args);
                case "emote":
                case "me":
                    return new EmoteCommand(_raw, args);
                case "friends":
                case "friend":
                case "f":
                    return new FriendCommand(_raw, args);
                case "help":
                case "?":
                    return new HelpCommand(_raw, args);
                case "ignore":
                case "squelch":
                    return new SquelchCommand(_raw, args);
                case "kick":
                    return new KickCommand(_raw, args);
                case "rejoin":
                case "rj":
                    return new ReJoinCommand(_raw, args);
                case "time":
                    return new TimeCommand(_raw, args);
                case "unban":
                    return new UnBanCommand(_raw, args);
                case "unignore":
                case "unsquelch":
                    return new UnsquelchCommand(_raw, args);
                case "users":
                    return new UsersCommand(_raw, args);
                case "version":
                case "ver":
                    return new VersionCommand(_raw, args);
                case "whereis":
                case "where":
                case "whois":
                    return new WhereIsCommand(_raw, args);
                case "whisper":
                case "msg":
                case "m":
                case "w":
                    return new WhisperCommand(_raw, args);
                case "who":
                    return new WhoCommand(_raw, args);
                case "whoami":
                    return new WhoAmICommand(_raw, args);
                default:
                    return new InvalidCommand(_raw, args);
            }
        }
    }
}
