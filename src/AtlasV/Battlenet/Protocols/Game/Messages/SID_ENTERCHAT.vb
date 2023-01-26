Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_ENTERCHAT
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_ENTERCHAT)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_ENTERCHAT)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")

                    If Buffer.Length < 2 Then
                        Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 2 bytes")
                    End If

                    If context.Client.GameState.ActiveAccount Is Nothing OrElse String.IsNullOrEmpty(context.Client.GameState.OnlineName) Then
                        Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} received before logon")
                    End If

                    Dim username(), statstring() As Byte
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            username = r.ReadByteString()
                            statstring = r.ReadByteString()
                        End Using
                    End Using

                    Dim productId = CUInt(context.Client.GameState.Product)
                    If statstring.Length <> 0 AndAlso (statstring.Length < 4 OrElse statstring.Length > 128) Then Throw New GameProtocolViolationException(context.Client, $"Client sent invalid statstring size in {MessageName(Id)}")

                    If statstring.Length < 4 Then
                        statstring = New Byte(3) {}
                    End If

                    Using _m = New MemoryStream(statstring)
                        Using _w = New BinaryWriter(_m)
                            _w.BaseStream.Position = 0
                            _w.Write(productId)
                        End Using
                    End Using

                    If Product.IsDiablo(context.Client.GameState.Product) Then
                        context.Client.GameState.Statstring = statstring
                    Else
                        context.Client.GameState.GenerateStatstring()
                    End If

                    Return New SID_ENTERCHAT().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
                Case MessageDirection.ServerToClient
                    Dim uniqueName = context.Client.GameState.OnlineName
                    Dim statstring = context.Client.GameState.Statstring
                    Dim accountName = context.Client.GameState.Username
                    Buffer = New Byte(3 + Encoding.UTF8.GetByteCount(uniqueName) + statstring.Length + Encoding.UTF8.GetByteCount(accountName) - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CStr(uniqueName))
                            w.WriteByteString(statstring)
                            w.Write(CStr(accountName))
                        End Using
                    End Using

                    SyncLock context.Client.GameState
                        context.Client.GameState.ChannelFlags = CType(context.Client.GameState.ActiveAccount.[Get](Account.FlagsKey), Account.Flags)

                        If Product.IsUDPSupported(context.Client.GameState.Product) AndAlso Not context.Client.GameState.UDPSupported Then
                            context.Client.GameState.ChannelFlags = context.Client.GameState.ChannelFlags Or Account.Flags.NoUDP
                        End If
                    End SyncLock

                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    context.Client.Send(ToByteArray(context.Client.ProtocolType))
                    Return True
            End Select

            Return False
        End Function
    End Class
End Namespace
