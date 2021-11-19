Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_STARTVERSIONING
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_STARTVERSIONING)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_STARTVERSIONING)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")

            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    If Buffer.Length <> 16 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 16 bytes")

                    Dim unknown0 As UInt32
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            context.Client.GameState.Platform = CType(r.ReadUInt32(), Platform.PlatformCode)
                            context.Client.GameState.Product = CType(r.ReadUInt32(), Product.ProductCode)
                            context.Client.GameState.Version.VersionByte = r.ReadUInt32()
                            unknown0 = r.ReadUInt32()
                        End Using
                    End Using

                    If unknown0 <> 0 Then Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, String.Format("[" & Common.DirectionToString(context.Direction) & "] {MessageName(Id)} unknown field is non-zero (0x{0:X8})", unknown0))
                    Return New SID_STARTVERSIONING().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
                Case MessageDirection.ServerToClient
                    Dim MPQFiletime As ULong = 0
                    Dim MPQFilename As String = "ver-IX86-1.mpq"
                    Dim Formula As Byte() = Encoding.UTF8.GetBytes("A=3845581634 B=880823580 C=1363937103 4 A=A-S B=B-C C=C-A A=A-B")
                    Dim fileinfo = New BNFTP.File(MPQFilename).GetFileInfo()

                    If fileinfo Is Nothing Then
                        Logging.WriteLine(Logging.LogLevel.[Error], Logging.LogType.Client_Game, $"Version check file [{MPQFilename}] does not exist!")
                    Else
                        MPQFilename = fileinfo.Name
                        MPQFiletime = CULng(fileinfo.LastWriteTimeUtc.ToFileTimeUtc())
                    End If

                    Buffer = New Byte(10 + Encoding.UTF8.GetByteCount(MPQFilename) + Formula.Length - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CULng(MPQFiletime))
                            w.Write(CStr(MPQFilename))
                            w.WriteByteString(Formula)
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
