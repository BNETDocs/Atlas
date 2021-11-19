Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_GAMERESULT
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_GAMERESULT)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_GAMERESULT)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length < 10 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 10 bytes")

            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    Dim gameType = r.ReadUInt32()
                    Dim resultCount = r.ReadUInt32()
                    Dim results = New List(Of UInt32)()
                    Dim players = New List(Of Byte())()

                    For i = 0 To resultCount - 1
                        results.Add(r.ReadUInt32())
                    Next

                    For i = 0 To resultCount - 1
                        players.Add(r.ReadByteString())
                    Next

                    Dim mapName = r.ReadByteString()
                    Dim playerScore = r.ReadByteString()
                End Using
            End Using

            Return True
        End Function
    End Class
End Namespace
