Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_GETICONDATA
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_GETICONDATA)
            Buffer = New Byte(15) {}
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_GETICONDATA)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")

            Select Case context.Direction
                Case MessageDirection.ClientToServer

                    If Buffer.Length <> 0 Then
                        Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 0 bytes")
                    End If

                    Return New SID_GETICONDATA().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
                Case MessageDirection.ServerToClient
                    Dim fileInfo = New FileInfo("icons.bni")
                    Dim fileTime = CULng(fileInfo.LastWriteTimeUtc.ToFileTimeUtc())
                    Dim fileName = fileInfo.Name
                    Buffer = New Byte(9 + Encoding.UTF8.GetByteCount(fileName) - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CULng(fileTime))
                            w.Write(fileName)
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
