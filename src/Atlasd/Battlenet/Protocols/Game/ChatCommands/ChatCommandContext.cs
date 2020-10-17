namespace Atlasd.Battlenet.Protocols.Game
{
    class ChatCommandContext
    {
        public ChatCommand Command { get; protected set; }
        public GameState GameState { get; protected set; }

        public ChatCommandContext(ChatCommand command, GameState gameState)
        {
            Command = command;
            GameState = gameState;
        }
    }
}
