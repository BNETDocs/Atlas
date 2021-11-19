Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CLICKAD
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_CLICKAD)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CLICKAD)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length <> 8 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 8 bytes")

            Dim adId, requestType As UInt32
            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    adId = r.ReadUInt32()
                    requestType = r.ReadUInt32()
                End Using
            End Using

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Ad [Id: 0x{adId}] was clicked!")
            Return True
        End Function
    End Class
End Namespace
