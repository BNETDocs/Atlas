Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CHECKAD
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_CHECKAD)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CHECKAD)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length <> 16 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 16 bytes")

                    Dim platformID As UInt32
                    Dim productID As UInt32
                    Dim lastShownAdId As UInt32
                    Dim currentTime As UInt32

                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            platformID = r.ReadUInt32()
                            productID = r.ReadUInt32()
                            lastShownAdId = r.ReadUInt32()
                            currentTime = r.ReadUInt32()
                        End Using
                    End Using

                    Return New SID_CHECKAD().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                        {"platformId", platformID},
                        {"productId", productID},
                        {"lastShownAdId", lastShownAdId},
                        {"currentTime", currentTime}
                    }))
                Case MessageDirection.ServerToClient
                    Dim rand = New Random()
                    Dim adId As UInteger
                    Dim ad As Advertisement

                    SyncLock Battlenet.Common.ActiveAds
                        adId = CUInt(rand.[Next](0, Battlenet.Common.ActiveAds.Count - 1))
                        ad = Battlenet.Common.ActiveAds(CInt(adId))
                    End SyncLock

                    Buffer = New Byte(18 + Encoding.UTF8.GetByteCount(ad.Filename) + Encoding.UTF8.GetByteCount(ad.Url) - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(adId))
                            w.Write(CUInt(0))
                            w.Write(CULng(ad.Filetime.ToFileTimeUtc()))
                            w.Write(CStr(ad.Filename))
                            w.Write(CStr(ad.Url))
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
