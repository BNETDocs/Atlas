using Atlasd.Battlenet.Protocols.Game.ChatCommands;
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

        public bool CanInvoke(ChatCommandContext context)
        {
            //return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
            return false;
        }

        public void Invoke(ChatCommandContext context)
        {
            throw new NotSupportedException("Base ChatCommand class does not Invoke()");
        }

        public static ChatCommand FromString(string text)
        {
            if (text[0] != '/')
                return new ChatCommand(new List<string>() { text });

            var args = text[1..].Split(' ');

            switch (args[0])
            {
                case "help": case "?":
                    return new HelpCommand(new List<string>(args));
            }

            return new InvalidCommand(new List<string>(args));
        }
    }
}
