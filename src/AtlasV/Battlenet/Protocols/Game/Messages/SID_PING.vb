Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_PING
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_PING)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_PING)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")

            If context.Arguments IsNot Nothing AndAlso context.Arguments.ContainsKey("token") Then
                Dim t = CUInt(context.Arguments("token"))
                Buffer = New Byte(3) {}

                Using _m = New MemoryStream(Buffer)
                    Using _w = New BinaryWriter(_m)
                        _w.Write(t)
                    End Using
                End Using
            End If

            If Buffer.Length <> 4 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes")
            If context.Client Is Nothing OrElse Not context.Client.Connected Then Return False
            Dim delta = DateTime.Now - context.Client.GameState.PingDelta
            Dim autoRefreshPings = Settings.GetBoolean(New String() {"battlenet", "emulation", "auto_refresh_pings"}, False)
            If Not autoRefreshPings AndAlso context.Client.GameState.Ping <> -1 Then Return True

            Dim token As UInt32
            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    token = r.ReadUInt32()
                End Using
            End Using

            If Not (context.Direction = MessageDirection.ClientToServer AndAlso token = context.Client.GameState.PingToken) Then
                Return True
            End If
            context.Client.GameState.Ping = CInt(Math.Round(delta.TotalMilliseconds))
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Ping: {context.Client.GameState.Ping}ms")

            If context.Client.GameState.ActiveChannel IsNot Nothing Then
                context.Client.GameState.ActiveChannel.UpdateUser(context.Client.GameState, context.Client.GameState.Ping)
            End If

            Return True
        End Function
    End Class
End Namespace
