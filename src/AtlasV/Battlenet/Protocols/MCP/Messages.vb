Namespace AtlasV.Battlenet.Protocols.MCP
    Enum Messages As Byte
        MCP_STARTUP = &H1
        MCP_CHARCREATE = &H2
        MCP_CREATEGAME = &H3
        MCP_JOINGAME = &H4
        MCP_GAMELIST = &H5
        MCP_GAMEINFO = &H6
        MCP_CHARLOGON = &H7
        MCP_CHARDELETE = &HA
        MCP_REQUESTLADDERDATA = &H11
        MCP_MOTD = &H12
        MCP_CANCELGAMECREATE = &H13
        MCP_CREATEQUEUE = &H14
        MCP_CHARRANK = &H16
        MCP_CHARLIST = &H17
        MCP_CHARUPGRADE = &H18
        MCP_CHARLIST2 = &H19
    End Enum
End Namespace