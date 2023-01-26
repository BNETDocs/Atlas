Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CHATEVENT
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_CHATEVENT)
            Buffer = New Byte(25) {}
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CHATEVENT)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            If context.Direction = MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"Client is not allowed to send {MessageName(Id)}")
            Dim chatEvent = CType(context.Arguments("chatEvent"), ChatEvent)
            Buffer = chatEvent.ToByteArray(context.Client.ProtocolType.Type)
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)}: {ChatEvent.EventIdToString(chatEvent.EventId)} ({4 + Buffer.Length} bytes)")
            Return True
        End Function
    End Class
End Namespace
