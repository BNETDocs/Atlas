Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_REPORTVERSION
        Inherits Message

        Public Enum ResultIds As UInt32
            Failed = 0
            OldGame = 1
            Success = 2
            Reinstall = 3
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_REPORTVERSION)
            Buffer = New Byte(5) {}
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_REPORTVERSION)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")

            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    If Buffer.Length < 21 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 21 bytes")

                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            context.Client.GameState.Platform = CType(r.ReadUInt32(), Platform.PlatformCode)
                            context.Client.GameState.Product = CType(r.ReadUInt32(), Product.ProductCode)
                            context.Client.GameState.Version.VersionByte = r.ReadUInt32()
                            context.Client.GameState.Version.EXERevision = r.ReadUInt32()
                            context.Client.GameState.Version.EXEChecksum = r.ReadUInt32()
                            context.Client.GameState.Version.EXEInformation = r.ReadByteString()
                        End Using
                    End Using

                    Return New SID_REPORTVERSION().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
                Case MessageDirection.ServerToClient
                    Dim filename = "ver-IX86-1.mpq"
                    Buffer = New Byte(6 + Encoding.UTF8.GetByteCount(filename) - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(ResultIds.Success))
                            w.Write(CStr(filename))
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
