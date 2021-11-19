Imports AtlasV.Daemon

Namespace AtlasV.Battlenet
    Public Class ProtocolType

        Public Enum Types As Byte
            Game = &H1
            BNFTP = &H2
            Chat = &H3
            Chat_Alt1 = &H43
            Chat_Alt2 = &H63
            IPC = &H80
            Letter_C = &H43
            Letter_G = &H47
            Letter_H = &H48
            Letter_O = &H4F
            Letter_P = &H50
        End Enum


        Public Property Type As Types

        Public Sub New(ByVal varType As Types)
            Type = varType
        End Sub

        Public Function IsBNFTP() As Boolean
            Return Type = Types.BNFTP
        End Function

        Public Function IsChat() As Boolean
            Return Type = Types.Chat OrElse Type = Types.Chat_Alt1 OrElse Type = Types.Chat_Alt2
        End Function

        Public Function IsGame() As Boolean
            Return Type = Types.Game
        End Function

        Public Function IsInterProcessCommunication() As Boolean
            Return Type = Types.IPC
        End Function

        Public Function IsIPC() As Boolean
            Return IsInterProcessCommunication()
        End Function

        Public Shared Function ProtocolTypeName(ByVal varType As Types) As String
            Select Case varType
                Case Types.Game : Return "Game"
                Case Types.BNFTP : Return "BNFTP"
                Case Types.Chat : Return "Chat"
                Case Types.Chat_Alt1 : Return "Chat_Alt1"
                Case Types.Chat_Alt2 : Return "Chat_Alt2"
                Case Types.IPC : Return "IPC"
                Case Else : Return $"Unknown (0x{CByte(varType)})"
            End Select
        End Function

        Public Shared Function ProtocolTypeToLogType(varType As Types) As UInt32
            Select Case varType
                Case Types.Game : Return Logging.LogType.Client_Game
                Case Types.BNFTP : Return Logging.LogType.Client_BNFTP
                Case Types.Chat : Return Logging.LogType.Client_Chat
                Case Types.Chat_Alt1 : Return Logging.LogType.Client_Chat
                Case Types.Chat_Alt2 : Return Logging.LogType.Client_Chat
                Case Types.IPC : Return Logging.LogType.Client_IPC
                Case Else : Return Logging.LogType.Client
            End Select
        End Function

        Public Overrides Function ToString() As String
            Return ProtocolTypeName(Type)
        End Function

    End Class
End Namespace
