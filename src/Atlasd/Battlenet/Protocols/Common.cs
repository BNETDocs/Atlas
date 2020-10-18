namespace Atlasd.Battlenet.Protocols
{
    class Common
    {
        public const string HumanDateTimeFormat = "ddd MMM dd hh:mm tt"; // example: "Sat Oct 17  6:11 AM"

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
