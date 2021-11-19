Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_READMEMORY
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_READMEMORY)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_READMEMORY)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 4 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 4 bytes")

                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            Dim requestId = r.ReadUInt32()
                            Dim data = r.ReadBytes(CInt((r.BaseStream.Length - r.BaseStream.Position)))
                        End Using
                    End Using

                    Exit Select
                Case MessageDirection.ServerToClient
                    Dim requestId = CUInt(context.Arguments("requestId"))
                    Dim address = CUInt(context.Arguments("address"))
                    Dim length = CUInt(context.Arguments("length"))
                    Buffer = New Byte(11) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(requestId))
                            w.Write(CUInt(address))
                            w.Write(CUInt(length))
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
