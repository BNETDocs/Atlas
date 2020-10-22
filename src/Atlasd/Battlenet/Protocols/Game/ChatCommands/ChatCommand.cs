using Atlasd.Battlenet.Protocols.Game.ChatCommands;
using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game
{
    class ChatCommand
    {
        public List<string> Arguments { get; protected set; }

        public ChatCommand(List<string> arguments)
        {
            Arguments = arguments;
        }

        public virtual bool CanInvoke(ChatCommandContext context)
        {
            return false;
        }

        public virtual void Invoke(ChatCommandContext context)
        {
            throw new NotSupportedException("Base ChatCommand class does not Invoke()");
        }

        public static ChatCommand FromString(string text)
        {
            var args = new List<string>(text.Split(' '));

            var cmd = args[0];
            args.RemoveAt(0);

            switch (cmd)
            {
                case "channel":
                case "join":
                case "j":
                    return new JoinCommand(args);
                case "help":
                case "?":
                    return new HelpCommand(args);
                case "kick":
                    return new KickCommand(args);
                case "time":
                    return new TimeCommand(args);
                case "who":
                    return new WhoCommand(args);
                case "whoami":
                    return new WhoAmICommand(args);
                default:
                    return new InvalidCommand(args);
            }
        }
    }
}
