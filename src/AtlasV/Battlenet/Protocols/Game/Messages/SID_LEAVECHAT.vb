Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_LEAVECHAT
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_LEAVECHAT)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_LEAVECHAT)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length <> 0 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 0 bytes")

            Try

                SyncLock context.Client.GameState.ActiveChannel
                    context.Client.GameState.ActiveChannel.RemoveUser(context.Client.GameState)
                End SyncLock

            Catch __unusedArgumentNullException1__ As ArgumentNullException
            Catch __unusedNullReferenceException2__ As NullReferenceException
            End Try

            Return True
        End Function
    End Class
End Namespace
