Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CLANMEMBERSTATUSCHANGE
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_CLANMEMBERSTATUSCHANGE)
            Buffer = New Byte(5) {}
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CLANMEMBERSTATUSCHANGE)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            If context.Direction <> MessageDirection.ServerToClient Then
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client")
            End If

            Dim username As Byte() = CType(context.Arguments("username"), Byte())
            Dim rank As Byte = CByte(context.Arguments("rank"))
            Dim status As Byte = CByte(context.Arguments("status"))
            Dim location As Byte() = CType(context.Arguments("location"), Byte())
            Buffer = New Byte(4 + username.Length + location.Length - 1) {}

            Using m = New MemoryStream(Buffer)
                Using w = New BinaryWriter(m)
                    w.WriteByteString(username)
                    w.Write(rank)
                    w.Write(status)
                    w.WriteByteString(location)
                End Using
            End Using

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            context.Client.Send(ToByteArray(context.Client.ProtocolType))
            Return True
        End Function
    End Class
End Namespace
