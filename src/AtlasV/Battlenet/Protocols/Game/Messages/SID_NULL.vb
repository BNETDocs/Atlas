Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_NULL
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_NULL)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_NULL)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If Buffer.Length <> 0 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 0 bytes")
            Return True
        End Function
    End Class
End Namespace
