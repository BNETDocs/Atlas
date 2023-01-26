Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_GETFILETIME
        Inherits Message

        Public Enum RequestIds As UInt32
            TermsOfService_usa = &H1UI
            BnServerListW3 = &H3UI
            rereTermsOfService_USA = &H1AUI
            BnServerList = &H1BUI
            IconsSC = &H1DUI
            BnServerListD2 = &H80000004UI
            ExtraOptionalWorkIX86 = &H80000005UI
            ExtraRequiredWorkIX86 = &H80000006UI
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_GETFILETIME)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_GETFILETIME)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 9 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 9 bytes")

                    Dim requestId, unknown As UInt32
                    Dim filename As String
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            requestId = r.ReadUInt32()
                            unknown = r.ReadUInt32()
                            filename = r.ReadString()
                        End Using
                    End Using

                    Dim filetime = CULng(0)
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_BNFTP, context.Client.RemoteEndPoint, $"Requesting filetime for [{filename}]...")
                    Dim fileinfo = New BNFTP.File(filename).GetFileInfo()

                    If fileinfo IsNot Nothing Then
                        filename = fileinfo.Name
                        filetime = CULng(fileinfo.LastWriteTimeUtc.ToFileTimeUtc())
                    End If

                    Return New SID_GETFILETIME().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                        {"requestId", requestId},
                        {"unknown", unknown},
                        {"filetime", filetime},
                        {"filename", filename}
                    }))
                Case MessageDirection.ServerToClient
                    Dim requestId = CUInt(context.Arguments("requestId"))
                    Dim unknown = CUInt(context.Arguments("unknown"))
                    Dim filetime = CULng(context.Arguments("filetime"))
                    Dim filename = CStr(context.Arguments("filename"))
                    Buffer = New Byte(17 + Encoding.UTF8.GetByteCount(filename) - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(requestId))
                            w.Write(CUInt(unknown))
                            w.Write(CULng(filetime))
                            w.Write(CStr(filename))
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
