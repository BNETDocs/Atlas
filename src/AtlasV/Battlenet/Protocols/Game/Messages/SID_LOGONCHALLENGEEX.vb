Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_LOGONCHALLENGEEX
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_LOGONCHALLENGEEX)
            Buffer = New Byte(7) {}
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_LOGONCHALLENGEEX)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ServerToClient Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client")
            If Buffer.Length <> 8 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 8 bytes")

            Using m = New MemoryStream(Buffer)
                Using w = New BinaryWriter(m)
                    w.Write(CUInt(context.Client.GameState.UDPToken))
                    w.Write(CUInt(context.Client.GameState.ServerToken))
                End Using
            End Using

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            context.Client.Send(ToByteArray(context.Client.ProtocolType))
            Return True
        End Function
    End Class
End Namespace
