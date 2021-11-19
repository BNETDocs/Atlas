Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CHATCOMMAND
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_CHATCOMMAND)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CHATCOMMAND)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} may only be transmitted from client to server")
            If context.Client.GameState Is Nothing Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} requires a GameState object")
            If Buffer.Length < 2 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 2 bytes")
            If Buffer.Length > 224 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at most 224 bytes")

            'CHECK THIS SHIT
            Dim raw() As Byte = Buffer.Take(Buffer.Length - 1).ToArray() '= Buffer(0.. Xor 1) 'removes the null, the ^1 made no sense

            For Each c In raw
                If c < 32 Then
                    Return True
                End If
            Next

            If Convert.ToChar(raw(0)) <> "/"c Then
                If context.Client.GameState.ActiveChannel Is Nothing Then Throw New GameProtocolViolationException(context.Client, "Cannot send message, user is not in a channel")

                If context.Client.GameState.ActiveChannel.Count <= 1 OrElse context.Client.GameState.ActiveChannel.ActiveFlags.HasFlag(Channel.Flags.Silent) Then
                    Call New ChatEvent(ChatEvent.EventIds.EID_INFO, context.Client.GameState.ActiveChannel.ActiveFlags, 0, context.Client.GameState.ActiveChannel.Name, Resources.NoOneHearsYou).WriteTo(context.Client)
                Else
                    context.Client.GameState.ActiveChannel.WriteChatMessage(context.Client.GameState, raw, False)
                End If

                Return True
            End If

            Dim onlineName = context.Client.GameState.OnlineName
            Dim command = ChatCommand.FromByteArray(raw.Skip(1).ToArray()) 'raw(1..))
            Dim commandEnvironment = New Dictionary(Of String, String)() From {
                {"accountName", context.Client.GameState.Username},
                {"channel", If(context.Client.GameState.ActiveChannel Is Nothing, "(null)", context.Client.GameState.ActiveChannel.Name)},
                {"game", Product.ProductName(context.Client.GameState.Product, True)},
                {"host", "BNETDocs"},
                {"localTime", context.Client.GameState.LocalTime.ToString(Common.HumanDateTimeFormat)},
                {"name", onlineName},
                {"onlineName", onlineName},
                {"realm", "Battle.net"},
                {"realmTime", DateTime.Now.ToString(Common.HumanDateTimeFormat)},
                {"realmTimezone", $"UTC{DateTime.Now}"},
                {"user", onlineName},
                {"username", onlineName},
                {"userName", onlineName}
            }
            Dim commandContext = New ChatCommandContext(command, commandEnvironment, context.Client.GameState)

            If Not command.CanInvoke(commandContext) Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, context.Client.GameState.ChannelFlags, context.Client.GameState.Ping, context.Client.GameState.OnlineName, Resources.ChatCommandUnavailable).WriteTo(context.Client)
                Return True
            End If

            command.Invoke(commandContext)
            Return True
        End Function
    End Class
End Namespace
