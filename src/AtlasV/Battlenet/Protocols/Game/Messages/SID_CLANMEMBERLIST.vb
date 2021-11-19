Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CLANMEMBERLIST
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_CLANMEMBERLIST)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CLANMEMBERLIST)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")

                    If context.Client.GameState.ActiveAccount Is Nothing Then
                        Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} received before logon")
                    End If

                    If Buffer.Length <> 4 Then
                        Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes")
                    End If

                    Dim cookie As UInt32
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            cookie = r.ReadUInt32()
                        End Using
                    End Using

                    Return New SID_CLANMEMBERLIST().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                        {"cookie", cookie}
                    }))
                Case MessageDirection.ServerToClient
                    Dim cookie = CUInt(context.Arguments("cookie"))
                    Buffer = New Byte(4) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(cookie)
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
