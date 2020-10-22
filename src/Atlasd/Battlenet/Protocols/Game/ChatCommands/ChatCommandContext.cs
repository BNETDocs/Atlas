using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game
{
    class ChatCommandContext
    {
        public ChatCommand Command { get; protected set; }
        public Dictionary<string, string> Environment { get; protected set; }
        public GameState GameState { get; protected set; }

        public ChatCommandContext(ChatCommand command, Dictionary<string, string> environment, GameState gameState)
        {
            Command = command;
            Environment = environment;
            GameState = gameState;
        }
    }
}
