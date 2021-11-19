Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_REGISTRY
        Inherits Message

        Public Enum HiveKeyIds As UInt32
            HKEY_CLASSES_ROOT = &H80000000UI
            HKEY_CURRENT_USER = &H80000001UI
            HKEY_LOCAL_MACHINE = &H80000002UI
            HKEY_USERS = &H80000003UI
            HKEY_PERFORMANCE_DATA = &H80000004UI
            HKEY_CURRENT_CONFIG = &H80000005UI
            HKEY_DYN_DATA = &H80000006UI
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_REGISTRY)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_REGISTRY)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 5 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 5 bytes")

                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            Dim cookie = r.ReadUInt32()
                            Dim value = r.ReadByteString()
                        End Using
                    End Using

                    Return True
                Case MessageDirection.ServerToClient
                    Dim cookie = CUInt(context.Arguments("cookie"))
                    Dim hiveKeyId = CUInt(context.Arguments("hiveKeyId"))
                    Dim keyPath = CStr(context.Arguments("keyPath"))
                    Dim keyName = CStr(context.Arguments("keyName"))
                    Buffer = New Byte(10 + Encoding.UTF8.GetByteCount(keyPath) + Encoding.UTF8.GetByteCount(keyName) - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(cookie))
                            w.Write(CUInt(hiveKeyId))
                            w.Write(CStr(keyPath))
                            w.Write(CStr(keyName))
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
