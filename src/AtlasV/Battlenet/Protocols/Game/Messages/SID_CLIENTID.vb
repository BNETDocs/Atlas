Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CLIENTID
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_CLIENTID)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CLIENTID)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")

            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    If Buffer.Length < 18 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 18 bytes")
                    Return New SID_CLIENTID().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient)) AndAlso New SID_LOGONCHALLENGEEX().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient)) AndAlso New SID_PING().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New System.Collections.Generic.Dictionary(Of String, Object)() From {
                        {"token", context.Client.GameState.PingToken}
                    }))
                Case MessageDirection.ServerToClient
                    Buffer = New Byte(15) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(0))
                            w.Write(CUInt(0))
                            w.Write(CUInt(0))
                            w.Write(CUInt(0))
                        End Using
                    End Using

                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    context.Client.Send(ToByteArray(context.Client.ProtocolType))
                    Return True
            End Select

            Return False
        End Function
    End Class
End Namespace
