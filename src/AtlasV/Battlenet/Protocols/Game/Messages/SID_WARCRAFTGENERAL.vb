Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_WARCRAFTGENERAL
        Inherits Message

        Public Enum SubCommands As Byte
            WID_GAMESEARCH = &H0
            WID_MAPLIST = &H2
            WID_CANCELSEARCH = &H3
            WID_USERRECORD = &H4
            WID_TOURNAMENT = &H7
            WID_CLANRECORD = &H8
            WID_ICONLIST = &H9
            WID_SETICON = &HA
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_WARCRAFTGENERAL)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_WARCRAFTGENERAL)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Client.GameState Is Nothing OrElse Not Product.IsWarcraftIII(context.Client.GameState.Product) Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} is Warcraft III game client exclusive")
            If Buffer.Length < 1 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 1 byte")
            Dim subcommand As Byte

            Using m = New MemoryStream(Buffer)

                Using r = New BinaryReader(m)
                    subcommand = r.ReadByte()
                End Using
            End Using

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} received subcommand {subcommand}")
            Return True
        End Function
    End Class
End Namespace
