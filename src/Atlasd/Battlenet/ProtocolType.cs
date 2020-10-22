namespace Atlasd.Battlenet
{
    public class ProtocolType
    {
        public enum Types : byte
        {
            // Descriptive Protocols:
            Game      = 0x01, // Diablo, StarCraft, WarCraft
            BNFTP     = 0x02, // FTP server for Game protocol
            Chat      = 0x03, // Human text-based Chat gateway
            Chat_Alt1 = 0x43,
            Chat_Alt2 = 0x63,
            IPC       = 0x80, // Inter-Process Communication

            // Letterized Protocols:
            Letter_C = 0x43,
            Letter_G = 0x47,
            Letter_H = 0x48,
            Letter_O = 0x4F,
            Letter_P = 0x50,
        };

        public Types Type { get; private set; }

        public ProtocolType(Types type)
        {
            Type = type;
        }

        public bool IsBNFTP()
        {
            return Type == Types.BNFTP;
        }

        public bool IsChat()
        {
            return
                Type == Types.Chat ||
                Type == Types.Chat_Alt1 ||
                Type == Types.Chat_Alt2
            ;
        }

        public bool IsGame()
        {
            return Type == Types.Game;
        }

        public bool IsInterProcessCommunication()
        {
            return Type == Types.IPC;
        }

        public bool IsIPC()
        {
            return IsInterProcessCommunication();
        }

        public static string ProtocolTypeName(Types type)
        {
            return type switch
            {
                Types.Game      => "Game",
                Types.BNFTP     => "BNFTP",
                Types.Chat      => "Chat",
                Types.Chat_Alt1 => "Chat_Alt1",
                Types.Chat_Alt2 => "Chat_Alt2",
                Types.IPC       => "IPC",
                _ => $"Unknown (0x{(byte)type:X2})",
            };
        }

        public override string ToString()
        {
            return ProtocolTypeName(Type);
        }
    }
}
