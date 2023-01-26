Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CLANINFO
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_CLANINFO)
            Buffer = New Byte(5) {}
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CLANINFO)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ServerToClient Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client")
            Dim unknown As Byte = 0
            Dim tag As Byte() = CType(context.Arguments("tag"), Byte())
            Dim rank As Byte = CByte(context.Arguments("rank"))
            If tag.Length <> 4 Then Throw New GameProtocolViolationException(context.Client, $"Clan tag must be exactly 4 bytes")
            Buffer = New Byte(2 + tag.Length - 1) {}

            Using m = New MemoryStream(Buffer)
                Using w = New BinaryWriter(m)
                    w.Write(unknown)
                    w.Write(tag)
                    w.Write(rank)
                End Using
            End Using

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            context.Client.Send(ToByteArray(context.Client.ProtocolType))
            Return True
        End Function
    End Class
End Namespace
