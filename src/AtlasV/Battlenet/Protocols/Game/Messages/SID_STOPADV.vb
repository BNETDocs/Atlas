Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_STOPADV
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_STOPADV)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_STOPADV)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length <> 0 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 0 bytes")
            If context.Client.GameState Is Nothing Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} was received without an active GameState")
            context.Client.GameState.StopGameAd()
            Return True
        End Function
    End Class
End Namespace
