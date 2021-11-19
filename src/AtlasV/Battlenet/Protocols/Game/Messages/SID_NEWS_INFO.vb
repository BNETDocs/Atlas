Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_NEWS_INFO
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_NEWS_INFO)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_NEWS_INFO)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length <> 4 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes")

                    Dim timestamp As UInt32
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            timestamp = r.ReadUInt32()
                        End Using
                    End Using

                    Return New SID_NEWS_INFO().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                        {"timestamp", timestamp}
                    }))
                Case MessageDirection.ServerToClient
                    Dim rereAccount = context.Client.GameState.ActiveAccount
                    Dim lastLogon = CDate(rereAccount.[Get](Account.LastLogonKey, DateTime.Now,))
                    Dim newsGreeting = Battlenet.Common.GetServerGreeting(context.Client)
                    Dim newsTimestamp = DateTime.Now
                    Buffer = New Byte(18 + Encoding.UTF8.GetByteCount(newsGreeting) - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CByte(1))
                            w.Write(CUInt(lastLogon.ToFileTimeUtc() >> 32))
                            w.Write(CUInt(newsTimestamp.ToFileTimeUtc() >> 32))
                            w.Write(CUInt(newsTimestamp.ToFileTimeUtc() >> 32))
                            w.Write(CUInt(newsTimestamp.ToFileTimeUtc() >> 32))
                            w.Write(CStr(newsGreeting))
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
