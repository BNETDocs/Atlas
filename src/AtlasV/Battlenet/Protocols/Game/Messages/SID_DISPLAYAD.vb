Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_DISPLAYAD
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_DISPLAYAD)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_DISPLAYAD)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length < 14 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 14 bytes")

            Dim platformId, productId, adId As UInt32
            Dim adFilename, adUrl As String
            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    platformId = r.ReadUInt32()
                    productId = r.ReadUInt32()
                    adId = r.ReadUInt32()
                    adFilename = r.ReadString()
                    adUrl = r.ReadString()
                End Using
            End Using

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Ad [Id: 0x{adId}] was displayed!")
            Return True
        End Function
    End Class
End Namespace
