Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CREATEACCOUNT
        Inherits Message

        Protected Enum Statuses As UInt32
            Failure = 0
            Success = 1
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_CREATEACCOUNT)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CREATEACCOUNT)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Dim __ As Account = Nothing

            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 21 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 21 bytes")

                    Dim passwordHash() As Byte, username As String
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            passwordHash = r.ReadBytes(20)
                            username = r.ReadString()
                        End Using
                    End Using

                    Dim _status As Account.CreateStatus = Account.TryCreate(username, passwordHash, __)
                    Dim status = If(_status = Account.CreateStatus.Success, Statuses.Success, Statuses.Failure)
                    Return New SID_CREATEACCOUNT().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                        {"status", status}
                    }))
                Case MessageDirection.ServerToClient
                    Buffer = New Byte(3) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(CType(context.Arguments("status"), Statuses)))
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
