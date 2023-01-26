Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CHECKDATAFILE2
        Inherits Message

        Public Enum Statuses As UInt32
            Unapproved = 0
            Approved = 1
            LadderApproved = 2
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_CHECKDATAFILE2)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CHECKDATAFILE2)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 25 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 25 bytes")

                    Dim fileSize, fileName, status As UInt32
                    Dim fileChecksum() As Byte
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            fileSize = r.ReadUInt32()
                            fileChecksum = r.ReadBytes(20)
                            fileName = Encoding.UTF8.GetString(r.ReadByteString())
                            status = Statuses.Unapproved
                        End Using
                    End Using

                    Return New SID_CHECKDATAFILE2().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                        {"status", status}
                    }))
                Case MessageDirection.ServerToClient
                    Buffer = New Byte(3) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(context.Arguments("status")))
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
