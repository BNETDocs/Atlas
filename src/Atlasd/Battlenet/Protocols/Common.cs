namespace Atlasd.Battlenet.Protocols
{
    class Common
    {
        public static string DirectionToString(MessageDirection direction)
        {
            return (direction) switch
            {
                MessageDirection.ClientToServer => "C>S",
                MessageDirection.ServerToClient => "S>C",
                MessageDirection.PeerToPeer => "P2P",
                _ => "???",
            };
        }
    }
}
