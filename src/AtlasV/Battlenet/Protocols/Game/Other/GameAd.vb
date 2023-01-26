Imports System

Namespace AtlasV.Battlenet.Protocols.Game
    Class GameAd
        Public Enum GameTypes As UInt16
            Melee = &H2
            FreeForAll = &H3
            FFA = FreeForAll
            OneVsOne = &H4
            CaptureTheFlag = &H5
            CTF = &H5
            Greed = &H6
            Slaughter = &H7
            SuddenDeath = &H8
            Ladder = &H9
            IronManLadder = &H10
            UseMapSettings = &HA
            UMS = UseMapSettings
            TeamMelee = &HB
            TeamFreeForAll = &HC
            TeamFFA = TeamFreeForAll
            TeamCaptureTheFlag = &HD
            TeamCTF = TeamCaptureTheFlag
            TopVsBottom = &HF
            PGL = &H20
        End Enum

        Public Enum LadderTypes As UInt32
            None = &H0
            Ladder = &H1
            IronManLadder = &H3
        End Enum

        <Flags>
        Public Enum StateFlags As UInt32
            None = &H0
            [Private] = &H1
            Full = &H2
            HasPlayers = &H4
            InProgress = &H8
            DisconnectIsLoss = &H10
            Replay = &H80
        End Enum

        Public Property ActiveStateFlags As StateFlags
        Public Property ElapsedTime As UInt32
        Public Property Client As GameState
        Public Property GamePort As UInt32
        Public Property GameType As GameTypes
        Public Property GameVersion As UInt32
        Public Property Name As Byte()
        Public Property Password As Byte()
        Public Property Statstring As Byte()
        Public Property SubGameType As UShort

        Public Sub New(ByVal varClient As GameState,
                       ByVal varName As Byte(),
                       ByVal varPassword As Byte(),
                       ByVal varStatstring As Byte(),
                       ByVal varGamePort As UInt32,
                       ByVal varGameType As GameTypes,
                       ByVal varSubGameType As UShort,
                       ByVal varGameVersion As UInt32)
            ActiveStateFlags = StateFlags.None
            Client = varClient
            ElapsedTime = 0
            GamePort = varGamePort
            GameType = varGameType
            GameVersion = varGameVersion
            Name = varName
            Password = varPassword
            Statstring = varStatstring
            SubGameType = varSubGameType
        End Sub

        Public Sub SetActiveStateFlags(ByVal newFlags As StateFlags)
            ActiveStateFlags = newFlags
        End Sub

        Public Sub SetElapsedTime(ByVal newElapsedTime As UInt32)
            ElapsedTime = newElapsedTime
        End Sub

        Public Sub SetGameType(ByVal newGameType As GameTypes)
            GameType = newGameType
        End Sub

        Public Sub SetGameVersion(ByVal newGameVersion As UInt32)
            GameVersion = newGameVersion
        End Sub

        Public Sub SetName(ByVal newName As Byte())
            Name = newName
        End Sub

        Public Sub SetPassword(ByVal newPassword As Byte())
            Password = newPassword
        End Sub

        Public Sub SetPort(ByVal newPort As UInt32)
            GamePort = newPort
        End Sub

        Public Sub SetStatstring(ByVal newStatstring As Byte())
            Statstring = newStatstring
        End Sub

        Public Sub SetSubGameType(ByVal newSubGameType As Byte)
            SubGameType = newSubGameType
        End Sub
    End Class
End Namespace