Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_UDPPINGRESPONSE
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_UDPPINGRESPONSE)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_UDPPINGRESPONSE)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length <> 4 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes")
            If Not (context.Client.GameState.Statstring Is Nothing OrElse context.Client.GameState.Statstring.Length = 0) Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} cannot be sent while in chat")
            Dim udpToken As UInt32

            Using m = New MemoryStream(Buffer)

                Using r = New BinaryReader(m)
                    udpToken = r.ReadUInt32()
                End Using
            End Using

            context.Client.GameState.UDPSupported = udpToken = &H626E6574

            If context.Client.GameState.UDPSupported AndAlso context.Client.GameState.ActiveChannel IsNot Nothing AndAlso context.Client.GameState.ChannelFlags.HasFlag(Account.Flags.NoUDP) Then
                context.Client.GameState.ActiveChannel.UpdateUser(context.Client.GameState, context.Client.GameState.ChannelFlags And Not Account.Flags.NoUDP)
            End If

            Return True
        End Function
    End Class
End Namespace
