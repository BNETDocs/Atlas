Imports AtlasV.Battlenet.Protocols.Game
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.BNFTP
    Class BNFTPState
        Public Client As ClientState
        Public HeaderLength As UInt16 = 0
        Public ProtocolVersion As UInt16 = 0
        Public PlatformId As Platform.PlatformCode = Platform.PlatformCode.None
        Public ProductId As Product.ProductCode = Product.ProductCode.None
        Public AdId As UInt32 = 0
        Public AdFileExtension As UInt32 = 0
        Public FileStartPosition As UInt32 = 0
        Public FileTime As UInt64 = 0
        Public FileName As String = Nothing
        Public ServerToken As UInt32 = 0
        Public ClientToken As UInt32 = 0
        Public GameKey As GameKey = Nothing

        Public Sub New(ByVal varClient As ClientState)
            Client = varClient
            ServerToken = CUInt(New Random().[Next](0, &H7FFFFFFF))
        End Sub

        Public Sub Receive(ByVal buffer As Byte())
            Using m = New MemoryStream(buffer)
                Using r = New BinaryReader(m)

                    HeaderLength = r.ReadUInt16()
                    ProtocolVersion = r.ReadUInt16()

                    Select Case ProtocolVersion
                        Case &H100
                            PlatformId = CType(r.ReadUInt32(), Platform.PlatformCode)
                            ProductId = CType(r.ReadUInt32(), Product.ProductCode)
                            AdId = r.ReadUInt32()
                            AdFileExtension = r.ReadUInt32()
                            FileStartPosition = r.ReadUInt32()
                            FileTime = r.ReadUInt64()
                            FileName = Encoding.UTF8.GetString(r.ReadByteString())
                            Dim file = New BNFTP.File(FileName)

                            If file Is Nothing Then
                                Client.Disconnect()
                            End If

                            Dim stream As BinaryReader = Nothing
                            Dim uploaded As Boolean = False

                            Try

                                If Not file.OpenStream() Then
                                    Client.Disconnect()
                                    Exit Select
                                End If

                                stream = New BinaryReader(file.StreamReader.BaseStream)
                                stream.BaseStream.Position = Math.Min(stream.BaseStream.Length, FileStartPosition)
                                Dim fileLength = CInt((stream.BaseStream.Length - stream.BaseStream.Position))
                                HeaderLength = CUShort((25 + Encoding.UTF8.GetByteCount(FileName)))
                                Dim outBuf = New Byte(HeaderLength - 1) {}

                                Using wm = New MemoryStream(outBuf)
                                    Using w = New BinaryWriter(wm)
                                        w.Write(CUShort(HeaderLength))
                                        w.Write(CUShort(0))
                                        w.Write(CUInt(fileLength))
                                        w.Write(CUInt(AdId))
                                        w.Write(CUInt(AdFileExtension))
                                        w.Write(CULng(New FileInfo(file.Path).LastWriteTimeUtc.ToFileTimeUtc()))
                                        w.Write(CStr(FileName))
                                    End Using
                                End Using

                                Write(outBuf)
                                Write(stream.ReadBytes(fileLength))
                                uploaded = True
                            Catch ex As Exception
                                If Not (TypeOf ex Is IOException OrElse TypeOf ex Is FileNotFoundException OrElse TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is PathTooLongException) Then Throw
                                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_BNFTP, Client.RemoteEndPoint, $"{ex.[GetType]().Name} error encountered for requested file [{FileName}]" & (If(String.IsNullOrEmpty(ex.Message), "", $"; message: {ex.Message}")))
                            Finally

                                If uploaded Then
                                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_BNFTP, Client.RemoteEndPoint, $"Uploaded file [{FileName}]")
                                End If

                                If stream IsNot Nothing Then
                                    stream.Close()
                                End If

                                Client.Disconnect()
                            End Try

                            Exit Select
                        Case &H200
                            PlatformId = CType(r.ReadUInt32(), Platform.PlatformCode)
                            ProductId = CType(r.ReadUInt32(), Product.ProductCode)
                            AdId = r.ReadUInt32()
                            AdFileExtension = r.ReadUInt32()
                            FileStartPosition = r.ReadUInt32()
                            FileTime = r.ReadUInt64()
                            FileName = Encoding.UTF8.GetString(r.ReadByteString())
                            Exit Select
                        Case Else
                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_BNFTP, $"Received unknown BNFTP protocol version [0x{ProtocolVersion}]")
                            Client.Disconnect("Unknown BNFTP protocol version")
                            Exit Select
                    End Select

                End Using
            End Using
        End Sub

        Public Sub Write(ByVal buffer As Byte())
            Client.Send(buffer)
        End Sub
    End Class
End Namespace