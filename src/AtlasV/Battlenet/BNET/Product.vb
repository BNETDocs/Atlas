Imports System
Imports System.Text

Namespace AtlasV.Battlenet
    Public Class Product
        Public Enum ProductCode As UInt32
            None = 0UI
            Chat = &H43484154UI
            DiabloII = &H44324456UI
            DiabloIILordOfDestruction = &H44325850UI
            DiabloRetail = &H4452544CUI
            DiabloShareware = &H44534852UI
            StarcraftBroodwar = &H53455850UI
            StarcraftJapanese = &H4A535452UI
            StarcraftOriginal = &H53544152UI
            StarcraftShareware = &H53534852UI
            WarcraftIIBNE = &H5732424EUI
            WarcraftIIIDemo = &H5733444DUI
            WarcraftIIIFrozenThrone = &H57335850UI
            WarcraftIIIReignOfChaos = &H57415233UI
        End Enum

        Public Shared Function IsChatRestricted(ByVal code As ProductCode) As Boolean
            Select Case code
                Case ProductCode.Chat, ProductCode.DiabloRetail, ProductCode.DiabloShareware, ProductCode.StarcraftShareware, ProductCode.WarcraftIIIDemo
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function IsChat(ByVal code As ProductCode) As Boolean
            Return code = ProductCode.Chat
        End Function

        Public Shared Function IsDiablo(ByVal code As ProductCode) As Boolean
            Select Case code
                Case ProductCode.DiabloRetail, ProductCode.DiabloShareware
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function IsDiabloII(ByVal code As ProductCode) As Boolean
            Select Case code
                Case ProductCode.DiabloII, ProductCode.DiabloIILordOfDestruction
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function IsStarcraft(ByVal code As ProductCode) As Boolean
            Select Case code
                Case ProductCode.StarcraftBroodwar, ProductCode.StarcraftJapanese, ProductCode.StarcraftOriginal, ProductCode.StarcraftShareware
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function IsUDPSupported(ByVal code As ProductCode) As Boolean
            Select Case code
                Case ProductCode.DiabloRetail, ProductCode.DiabloShareware, ProductCode.StarcraftBroodwar, ProductCode.StarcraftJapanese, ProductCode.StarcraftOriginal, ProductCode.StarcraftShareware, ProductCode.WarcraftIIBNE
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function IsWarcraftII(ByVal code As ProductCode) As Boolean
            Return code = ProductCode.WarcraftIIBNE
        End Function

        Public Shared Function IsWarcraftIII(ByVal code As ProductCode) As Boolean
            Select Case code
                Case ProductCode.WarcraftIIIDemo, ProductCode.WarcraftIIIReignOfChaos, ProductCode.WarcraftIIIFrozenThrone
                    Return True
                Case Else
                    Return False
            End Select
        End Function

#Disable Warning IDE0060 ' Remove unused parameter
        Public Shared Function ProductName(ByVal code As ProductCode, ByVal Optional extended As Boolean = True) As String
#Enable Warning IDE0060 ' Remove unused parameter
            Select Case code
                Case ProductCode.None : Return "None"
                Case ProductCode.Chat : Return "Chat"
                Case ProductCode.DiabloII : Return "Diablo II"
                Case ProductCode.DiabloIILordOfDestruction : Return "Diablo II Lords Of Destruction"
                Case ProductCode.DiabloRetail : Return "Diablo"
                Case ProductCode.DiabloShareware : Return "Diablo Shareware"
                Case ProductCode.StarcraftBroodwar : Return "Starcraft Broodwars"
                Case ProductCode.StarcraftJapanese : Return "Starcraft Japan"
                Case ProductCode.StarcraftOriginal : Return "Starcraft"
                Case ProductCode.StarcraftShareware : Return "Starcraft Shareware"
                Case ProductCode.WarcraftIIBNE : Return "Warcraft II Battle.net Edition"
                Case ProductCode.WarcraftIIIDemo : Return "Warcraft III Demo"
                Case ProductCode.WarcraftIIIFrozenThrone : Return "Warcraft III The Frozen Throne"
                Case ProductCode.WarcraftIIIReignOfChaos : Return "Warcraft III Reign Of Chaos"
                Case Else : Return "Unknown"
            End Select
        End Function

#Disable Warning IDE0060 ' Remove unused parameter
        Public Shared Function ProductChannelName(ByVal code As ProductCode, ByVal Optional extended As Boolean = True) As String
#Enable Warning IDE0060 ' Remove unused parameter
            Select Case code
                Case ProductCode.Chat : Return "Public Chat"
                Case ProductCode.DiabloII : Return "Diablo II"
                Case ProductCode.DiabloIILordOfDestruction : Return "Lords Of Destruction"
                Case ProductCode.DiabloRetail : Return "Diablo"
                Case ProductCode.DiabloShareware : Return "Diablo Shareware"
                Case ProductCode.StarcraftBroodwar : Return "Brood wars"
                Case ProductCode.StarcraftJapanese : Return "Starcraft"
                Case ProductCode.StarcraftOriginal : Return "Starcraft"
                Case ProductCode.StarcraftShareware : Return "Starcraft Shareware"
                Case ProductCode.WarcraftIIBNE : Return "Warcraft II"
                Case ProductCode.WarcraftIIIDemo : Return "Warcraft III"
                Case ProductCode.WarcraftIIIFrozenThrone : Return "Frozen Throne"
                Case ProductCode.WarcraftIIIReignOfChaos : Return "Warcraft III"
                Case Else : Return "The Void"
            End Select
        End Function

        Public Shared Function StringToProduct(ByVal product As String) As ProductCode
            Select Case product.ToUpper()
                Case "CHAT", "TAHC"
                    Return ProductCode.Chat
                Case "D2DV", "VD2D"
                    Return ProductCode.DiabloII
                Case "D2XP", "PX2D"
                    Return ProductCode.DiabloIILordOfDestruction
                Case "DRTL", "LTRD"
                    Return ProductCode.DiabloRetail
                Case "DSHR", "RHSD"
                    Return ProductCode.DiabloShareware
                Case "SEXP", "PXES"
                    Return ProductCode.StarcraftBroodwar
                Case "JSTR", "RTSJ"
                    Return ProductCode.StarcraftJapanese
                Case "STAR", "RATS"
                    Return ProductCode.StarcraftOriginal
                Case "SSHR", "RHSS"
                    Return ProductCode.StarcraftShareware
                Case "W2BN", "NB2W"
                    Return ProductCode.WarcraftIIBNE
                Case "W3DM", "MD3W"
                    Return ProductCode.WarcraftIIIDemo
                Case "W3XP", "PX3W"
                    Return ProductCode.WarcraftIIIFrozenThrone
                Case "WAR3", "3RAW"
                    Return ProductCode.WarcraftIIIReignOfChaos
                Case Else
                    Return ProductCode.None
            End Select
        End Function

    End Class
End Namespace
