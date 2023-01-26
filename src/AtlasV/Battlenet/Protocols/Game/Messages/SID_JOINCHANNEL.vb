Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_JOINCHANNEL
        Inherits Message

        Public Enum Flags As UInt32
            NoCreate = 0
            First = 1
            Forced = 2
            First_D2 = 5
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_JOINCHANNEL)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_JOINCHANNEL)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            If context.Direction = MessageDirection.ServerToClient Then Throw New GameProtocolViolationException(context.Client, $"Server isn't allowed to send {MessageName(Id)}")
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If Buffer.Length < 5 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 5 bytes")

            Dim rereFlags As Flags, channelName As String
            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    rereFlags = CType(r.ReadUInt32(), Flags)
                    channelName = r.ReadString()
                End Using
            End Using

            If channelName.Length < 1 Then Throw New GameProtocolViolationException(context.Client, "Channel name must be greater than zero")
            If channelName.Length > 31 Then channelName = channelName.Substring(0, 31)

            For Each c In channelName
                If CUInt(Convert.ToByte(c)) < 31 Then Throw New GameProtocolViolationException(context.Client, "Channel name must not have ASCII control characters")
            Next

            Dim userCountryAbbr = CStr(Nothing)
            Dim userFlags = Account.Flags.None
            Dim userPing = CInt(-1)
            Dim userName = CStr(Nothing)
            Dim userGame = Product.ProductCode.None

            Try

                SyncLock context.Client.GameState
                    userCountryAbbr = context.Client.GameState.Locale.CountryNameAbbreviated
                    userFlags = CType(context.Client.GameState.ActiveAccount.[Get](Account.FlagsKey), Account.Flags)
                    userPing = context.Client.GameState.Ping
                    userName = context.Client.GameState.OnlineName
                    userGame = context.Client.GameState.Product
                End SyncLock

            Catch ex As Exception
                If Not (TypeOf ex Is ArgumentNullException OrElse TypeOf ex Is NullReferenceException) Then Throw
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"{ex.[GetType]().Name} error occurred while processing {MessageName(Id)} for GameState object")
                Return False
            End Try

            Dim firstJoin = rereFlags = Flags.First OrElse rereFlags = Flags.First_D2
            If firstJoin Then channelName = $"{Product.ProductChannelName(userGame)} {userCountryAbbr}-1"
            Dim ignoreLimits = userFlags.HasFlag(Account.Flags.Employee) OrElse userFlags.HasFlag(Account.Flags.Admin)
            Dim rereChannel = Channel.GetChannelByName(channelName, False)

            If rereChannel Is Nothing AndAlso rereFlags = Flags.NoCreate Then
                Call New ChatEvent(ChatEvent.EventIds.EID_CHANNELFULL, Channel.Flags.None, userPing, userName, channelName).WriteTo(context.Client)
                Return True
            End If

            If rereChannel Is Nothing Then rereChannel = Channel.GetChannelByName(channelName, True)
            rereChannel.AcceptUser(context.Client.GameState, ignoreLimits, True)

            If firstJoin Then
                Dim account As Account
                Dim activeUserFlags As Account.Flags
                Dim activeUserPing As Integer
                Dim lastLogon As DateTime
                Dim onlineName As String

                Try
                    Dim gameState = context.Client.GameState

                    SyncLock gameState
                        account = gameState.ActiveAccount
                        activeUserFlags = gameState.ChannelFlags
                        activeUserPing = gameState.Ping
                        lastLogon = gameState.LastLogon
                        onlineName = gameState.OnlineName
                    End SyncLock

                Catch ex As Exception
                    If Not (TypeOf ex Is ArgumentNullException OrElse TypeOf ex Is NullReferenceException) Then Throw
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"{ex.[GetType]().Name} error occurred while processing {MessageName(Id)} for GameState object")
                    Return False
                End Try

                Dim serverGreeting = Battlenet.Common.GetServerGreeting(context.Client).Split(Environment.NewLine)

                For Each line In serverGreeting
                    Call New ChatEvent(ChatEvent.EventIds.EID_INFO, rereChannel.ActiveFlags, 0, rereChannel.Name, line).WriteTo(context.Client)
                Next

                If Product.IsChatRestricted(userGame) Then
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, activeUserFlags, activeUserPing, onlineName, Resources.GameProductIsChatRestricted).WriteTo(context.Client)
                End If

                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, activeUserFlags, activeUserPing, onlineName, Resources.LastLogonInfo.Replace("{timestamp}", lastLogon.ToString(Common.HumanDateTimeFormat))).WriteTo(context.Client)
                Dim failedLogins = context.Client.GameState.FailedLogons
                context.Client.GameState.FailedLogons = 0

                If failedLogins > 0 Then
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, activeUserFlags, activeUserPing, onlineName, Resources.FailedLogonAttempts.Replace("{count}", failedLogins.ToString("##,0"))).WriteTo(context.Client)
                End If
            End If

            Return True
        End Function
    End Class
End Namespace
