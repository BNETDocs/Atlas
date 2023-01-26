Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_AUTH_INFO
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_AUTH_INFO)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_AUTH_INFO)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 38 Then Throw New Exceptions.GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be at least 38 bytes")

                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            context.Client.GameState.ProtocolId = r.ReadUInt32()
                            context.Client.GameState.Platform = CType(r.ReadUInt32(), Platform.PlatformCode)
                            context.Client.GameState.Product = CType(r.ReadUInt32(), Product.ProductCode)
                            context.Client.GameState.Version.VersionByte = r.ReadUInt32()
                            context.Client.GameState.Locale.LanguageCode = r.ReadUInt32()
                            context.Client.GameState.LocalIPAddress = New IPAddress(r.ReadBytes(4))
                            context.Client.GameState.TimezoneBias = r.ReadInt32()
                            context.Client.GameState.Locale.UserLocaleId = r.ReadUInt32()
                            context.Client.GameState.Locale.UserLanguageId = r.ReadUInt32()
                            context.Client.GameState.Locale.CountryNameAbbreviated = r.ReadString()
                            context.Client.GameState.Locale.CountryName = r.ReadString()
                        End Using
                    End Using

                    Return New SID_PING().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                        {"token", context.Client.GameState.PingToken}
                    })) AndAlso New SID_AUTH_INFO().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
                    'Return New SID_AUTH_INFO().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
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

                    Buffer = New Byte(22 + Encoding.UTF8.GetByteCount(MPQFilename) + Formula.Length + (If(Product.IsWarcraftIII(context.Client.GameState.Product), 128, 0)) - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(context.Client.GameState.LogonType))
                            w.Write(CUInt(context.Client.GameState.ServerToken))
                            w.Write(CUInt(context.Client.GameState.UDPToken))
                            w.Write(CULng(MPQFiletime))
                            w.Write(CStr(MPQFilename))
                            w.Write(Formula)
                            w.Write(CByte(0))
                            If Product.IsWarcraftIII(context.Client.GameState.Product) Then
                                w.Write(New Byte(127) {})
                            End If
                        End Using
                    End Using

                    context.Client.GameState.SetLocale()
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    context.Client.Send(ToByteArray(context.Client.ProtocolType))
                    Return True
            End Select

            Return False
        End Function
    End Class
End Namespace
