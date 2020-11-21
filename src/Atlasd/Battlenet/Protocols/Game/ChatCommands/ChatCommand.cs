using Atlasd.Battlenet.Protocols.Game.ChatCommands;
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

            var newRaw = raw[(cmd.Length + 1)..]; // Removes (cmd+' ') from raw

            switch (cmd)
            {
                case "admin":
                    return new AdminCommand(newRaw, args);
                case "away":
                    return new AwayCommand(newRaw, args);
                case "channel":
                case "join":
                case "j":
                    return new JoinCommand(newRaw, args);
                case "emote":
                case "me":
                    return new EmoteCommand(newRaw, args);
                case "help":
                case "?":
                    return new HelpCommand(newRaw, args);
                case "ignore":
                case "squelch":
                    return new SquelchCommand(newRaw, args);
                case "kick":
                    return new KickCommand(newRaw, args);
                case "time":
                    return new TimeCommand(newRaw, args);
                case "unignore":
                case "unsquelch":
                    return new UnsquelchCommand(newRaw, args);
                case "users":
                    return new UsersCommand(newRaw, args);
                case "whereis":
                case "where":
                case "whois":
                    return new WhereIsCommand(newRaw, args);
                case "whisper":
                case "msg":
                case "m":
                case "w":
                    return new WhisperCommand(newRaw, args);
                case "who":
                    return new WhoCommand(newRaw, args);
                case "whoami":
                    return new WhoAmICommand(newRaw, args);
                default:
                    return new InvalidCommand(newRaw, args);
            }
        }
    }
}
