Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_GETCHANNELLIST
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_GETCHANNELLIST)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_GETCHANNELLIST)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length <> 4 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes")
                    Dim channels = New List(Of String)()

                    For Each pair In Battlenet.Common.ActiveChannels
                        channels.Add(pair.Value.Name)
                    Next

                    Return New SID_GETCHANNELLIST().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                        {"channels", channels}
                    }))
                Case MessageDirection.ServerToClient
                    Dim channels = CType(context.Arguments("channels"), List(Of String))
                    Dim size = CUInt(1)

                    For Each channel In channels
                        size += CUInt((1 + Encoding.UTF8.GetByteCount(channel)))
                    Next

                    Buffer = New Byte(size - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            For Each channel In channels
                                w.Write(CStr(channel))
                            Next

                            w.Write(CByte(0))
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
