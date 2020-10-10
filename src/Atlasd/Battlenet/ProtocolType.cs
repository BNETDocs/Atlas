namespace Atlasd.Battlenet
{
    enum ProtocolType
    {
        None = 0xffff,
        Game = 0x01,
        BNFTP = 0x02,
        Chat = 0x03,
        Chat_Alt1 = 0x43,
        Chat_Alt2 = 0x63,
        IPC = 0x80,
    }
}
