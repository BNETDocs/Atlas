namespace Atlasd.Battlenet.Protocols.MCP
{
    enum Messages
    {
        MCP_STARTUP = 0x01,
        MCP_CHARCREATE = 0x02,
        MCP_CREATEGAME = 0x03,
        MCP_JOINGAME = 0x04,
        MCP_GAMELIST = 0x05,
        MCP_GAMEINFO = 0x06,
        MCP_CHARLOGON = 0x07,
        MCP_CHARDELETE = 0x0A,
        MCP_REQUESTLADDERDATA = 0x11,
        MCP_MOTD = 0x12,
        MCP_CANCELGAMECREATE = 0x13,
        MCP_CREATEQUEUE = 0x14,
        MCP_CHARRANK = 0x16,
        MCP_CHARLIST = 0x17,
        MCP_CHARUPGRADE = 0x18,
        MCP_CHARLIST2 = 0x19,
    }
}