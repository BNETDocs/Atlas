Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_QUERYADURL
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_QUERYADURL)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_QUERYADURL)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length <> 4 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes")

                    Dim adId As UInt32
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            adId = r.ReadUInt32()
                        End Using
                    End Using

                    Dim ad As Advertisement

                    Try

                        SyncLock Battlenet.Common.ActiveAds
                            ad = Battlenet.Common.ActiveAds(CInt(adId))
                        End SyncLock

#Disable Warning CA2208 ' Instantiate argument exceptions correctly
                        If ad Is Nothing Then Throw New ArgumentOutOfRangeException()
#Enable Warning CA2208 ' Instantiate argument exceptions correctly
                    Catch __unusedArgumentOutOfRangeException1__ As ArgumentOutOfRangeException
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Received url query request for out of bounds ad id [0x{adId}]")
                        Return False
                    End Try

                    Return New SID_QUERYADURL().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                        {"adId", adId},
                        {"adUrl", ad.Url}
                    }))
                Case MessageDirection.ServerToClient
                    Dim adId = CUInt(context.Arguments("adId"))
                    Dim adUrl = CStr(context.Arguments("adUrl"))
                    Buffer = New Byte(5 + Encoding.UTF8.GetByteCount(adUrl) - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(adId))
                            w.Write(CStr(adUrl))
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
