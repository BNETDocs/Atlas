Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_FRIENDSPOSITION
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_FRIENDSPOSITION)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_FRIENDSPOSITION)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            If context.Direction <> MessageDirection.ServerToClient Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client")
            Buffer = New Byte(1) {}

            Using m = New MemoryStream(Buffer)
                Using w = New BinaryWriter(m)
                    w.Write(CByte(context.Arguments("old")))
                    w.Write(CByte(context.Arguments("new")))
                End Using
            End Using

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            context.Client.Send(ToByteArray(context.Client.ProtocolType))
            Return True
        End Function
    End Class
End Namespace
