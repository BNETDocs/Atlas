Imports AtlasV.Battlenet.Protocols.Game
Imports AtlasV.Battlenet.Protocols.Game.Messages
Imports AtlasV.Battlenet.Protocols.Udp
Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Net
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Namespace AtlasV.Battlenet
    Class Common
        Public Const NewLine As String = vbCrLf

        Public Structure ShutdownEvent
            Public Property AdminMessage As String
            Public Property Cancelled As Boolean
            Public Property EventDate As DateTime
            Public Property EventTimer As Timer

            Public Sub New(ByVal adminMessage As String, ByVal cancelled As Boolean, ByVal eventDate As DateTime, ByVal eventTimer As Timer)
                adminMessage = adminMessage
                cancelled = cancelled
                eventDate = eventDate
                eventTimer = eventTimer
            End Sub
        End Structure

        Public Shared AccountsDb As Dictionary(Of String, Account)
        Public Shared AccountsProcessing As List(Of String)
        Public Shared ActiveAccounts As Dictionary(Of String, Account)
        Public Shared ActiveAds As List(Of Advertisement)
        Public Shared ActiveChannels As Dictionary(Of String, Channel)
        Public Shared ActiveClientStates As List(Of ClientState)
        Public Shared ActiveGameAds As ConcurrentDictionary(Of Byte(), GameAd)
        Public Shared ActiveGameStates As Dictionary(Of String, GameState)

        'ChatFilter
        Public Shared ActiveChatFilter As ConcurrentDictionary(Of Byte(), GameAd)

        Public Shared Property DefaultAddress As IPAddress
        Public Shared Property DefaultPort As Integer
        Public Shared Property Listener As ServerSocket
        Public Shared Property ListenerAddress As IPAddress
        Public Shared Property ListenerEndPoint As IPEndPoint
        Public Shared Property ListenerPort As Integer
        Public Shared Property NullTimer As Timer
        Private Shared NullTimerLock As Boolean
        Public Shared Property NullTimerState As List(Of GameState)
        Public Shared Property PingTimer As Timer
        Private Shared PingTimerLock As Boolean
        Public Shared Property PingTimerState As List(Of GameState)
        Public Shared Property ScheduledShutdown As ShutdownEvent
        Public Shared Property UdpListener As UdpListener

        Public Shared Function GetServerGreeting(ByVal receiver As ClientState) As String
            Dim r = Resources.ChannelFirstJoinGreeting
            r = r.Replace("{host}", "BNETDocs")
            r = r.Replace("{serverStats}", GetServerStats(receiver))
            r = r.Replace("{realm}", "Battle.net")
            Return r
        End Function

        Public Shared Function GetServerStats(ByVal receiver As ClientState) As String
            If receiver Is Nothing OrElse receiver.GameState Is Nothing OrElse receiver.GameState.ActiveChannel Is Nothing Then Return ""
            Dim channel = receiver.GameState.ActiveChannel
            Dim numGameOnline = GetActiveClientCountByProduct(receiver.GameState.Product)
            Dim numGameAdvertisements = 0
            Dim numTotalOnline = ActiveClientStates.Count
            Dim numTotalAdvertisements = 0
            Dim strGame = Product.ProductName(receiver.GameState.Product, True)
            Dim r = Resources.ServerStatistics
            r = r.Replace("{channel}", channel.Name)
            r = r.Replace("{host}", "BNETDocs")
            r = r.Replace("{game}", strGame)
            r = r.Replace("{gameUsers}", numGameOnline.ToString("#,0"))
            r = r.Replace("{gameAds}", numGameAdvertisements.ToString("#,0"))
            r = r.Replace("{realm}", "Battle.net")
            r = r.Replace("{totalUsers}", numTotalOnline.ToString("#,0"))
            r = r.Replace("{totalGameAds}", numTotalAdvertisements.ToString("#,0"))
            Return r
        End Function

        Public Shared Sub Initialize()
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Initializing Battle.net common state")
            AccountsDb = New Dictionary(Of String, Account)(StringComparer.OrdinalIgnoreCase)
            AccountsProcessing = New List(Of String)()
            ActiveAccounts = New Dictionary(Of String, Account)(StringComparer.OrdinalIgnoreCase)
            ActiveChannels = New Dictionary(Of String, Channel)(StringComparer.OrdinalIgnoreCase)
            ActiveClientStates = New List(Of ClientState)()
            ActiveGameAds = New ConcurrentDictionary(Of Byte(), GameAd)()
            ActiveGameStates = New Dictionary(Of String, GameState)(StringComparer.OrdinalIgnoreCase)
            InitializeAds()
            ProfanityFilter.Initialize()
            DefaultAddress = IPAddress.Any
            DefaultPort = 6112
            InitializeListener()
            NullTimerLock = False
            PingTimerLock = False
            NullTimerState = New List(Of GameState)()
            PingTimerState = New List(Of GameState)()
            NullTimer = New Timer(AddressOf ProcessNullTimer, NullTimerState, 100, 100)
            PingTimer = New Timer(AddressOf ProcessPingTimer, PingTimerState, 100, 100)
            ScheduledShutdown = New ShutdownEvent(Nothing, True, DateTime.MinValue, Nothing)
            Daemon.Common.TcpNoDelay = Settings.GetBoolean(New String() {"battlenet", "listener", "tcp_nodelay"}, True)
        End Sub

        Public Shared Sub InitializeAds()
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Initializing advertisements")

            If ActiveAds Is Nothing OrElse ActiveAds.Count <> 0 Then
                ActiveAds = New List(Of Advertisement)()
            End If

            Dim adsJson = Nothing
            Settings.State.RootElement.TryGetProperty("ads", adsJson)
            Dim enabledJson = Nothing, filenameJson = Nothing, urlJson = Nothing, productsJson = Nothing, localesJson = Nothing

            For Each adJson In adsJson.EnumerateArray()
                adJson.TryGetProperty("enabled", enabledJson)
                adJson.TryGetProperty("filename", filenameJson)
                adJson.TryGetProperty("url", urlJson)
                adJson.TryGetProperty("product", productsJson)
                adJson.TryGetProperty("locale", localesJson)
                Dim enabled = enabledJson.GetBoolean()
                Dim filename = filenameJson.GetString()
                Dim url = urlJson.GetString()
                Dim products As List(Of Product.ProductCode) = Nothing
                Dim locales As List(Of UInteger) = Nothing

                If productsJson.ValueKind = JsonValueKind.Array Then

                    For Each productJson In productsJson.EnumerateArray()
                        Dim productStr = productJson.GetString()
                    Next
                End If

                If localesJson.ValueKind = JsonValueKind.Array Then

                    For Each localeJson In localesJson.EnumerateArray()
                        Dim localeId = localeJson.GetUInt32()
                    Next
                End If

                Dim ad = New Advertisement(filename, url, products, locales)

                SyncLock ActiveAds
                    ActiveAds.Add(ad)
                End SyncLock
            Next

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Config, $"Initialized {ActiveAds.Count} advertisements")
        End Sub

        Private Shared Sub InitializeListener()
            Dim battlenetJson = Nothing
            Settings.State.RootElement.TryGetProperty("battlenet", battlenetJson)
            Dim listenerJson = Nothing
            battlenetJson.TryGetProperty("listener", listenerJson)
            Dim interfaceJson = Nothing
            listenerJson.TryGetProperty("interface", interfaceJson)
            Dim portJson = Nothing
            listenerJson.TryGetProperty("port", portJson)
            Dim listenerAddressStr = interfaceJson.GetString()
            Dim listenerAddress As IPAddress = Nothing

            If Not IPAddress.TryParse(listenerAddressStr, listenerAddress) Then
                Logging.WriteLine(Logging.LogLevel.[Error], Logging.LogType.Server, $"Unable to parse IP address from [battlenet.listener.interface] with value [{listenerAddressStr}]; using any")
                listenerAddress = DefaultAddress
            End If

            listenerAddress = listenerAddress
            Dim port = Nothing
            portJson.TryGetInt32(port)
            ListenerPort = port
            Dim listenerEndPoint As IPEndPoint = Nothing

            If Not IPEndPoint.TryParse($"{listenerAddress}:{ListenerPort}", listenerEndPoint) Then
                Logging.WriteLine(Logging.LogLevel.[Error], Logging.LogType.Server, $"Unable to parse endpoint with value [{listenerAddress}:{ListenerPort}]")
                Return
            End If

            listenerEndPoint = listenerEndPoint
            UdpListener = New UdpListener(listenerEndPoint)
            Listener = New ServerSocket(listenerEndPoint)
        End Sub

        Public Shared Function GetActiveClientCountByProduct(ByVal productCode As Product.ProductCode) As UInteger
            Dim count = CUInt(0)

            SyncLock ActiveClientStates

                For Each client In ActiveClientStates
                    If client Is Nothing OrElse client.GameState Is Nothing OrElse client.GameState.Product = Product.ProductCode.None Then Continue For
                    If client.GameState.Product = productCode Then count += 1
                Next
            End SyncLock

            Return count
        End Function

        Public Shared Function GetClientByOnlineName(ByVal varTarget As String, <Out> ByRef varClient As GameState) As Boolean
            Dim t = varTarget

            If t.Contains("*"c) Then
                't = t((t.IndexOf("*"c) + 1)..)
                t = t.Substring(t.IndexOf("*"c) + 1)
            End If

            Dim i = Nothing

            If t.Contains("#"c) Then
                'Dim n = t((t.LastIndexOf("#"c) + 1)..)
                Dim n = t.Substring(t.LastIndexOf("#"c) + 1)

                If Not Integer.TryParse(n, i) OrElse i = 0 Then
                    't = t(0..t.LastIndexOf("#"c))
                    t = t.Substring(0, t.LastIndexOf("#"c))
                End If
            End If

            Dim retVal As Boolean
            SyncLock ActiveGameStates
                retVal = ActiveGameStates.TryGetValue(t, varClient)
            End SyncLock
            Return retVal
        End Function

        Private Shared Sub ProcessNullTimer(ByVal state As Object)
            If NullTimerLock Then Return
            NullTimerLock = True

            Try
                Dim locClients = TryCast(state, List(Of GameState))
                Dim msg = New SID_NULL()
                Dim interval = TimeSpan.FromSeconds(60)
                Dim now = DateTime.Now

                SyncLock locClients

                    For Each client In locClients

                        If client Is Nothing Then
                            locClients.Remove(client)
                            Continue For
                        End If

                        SyncLock client
                            If client?.LastNull Is Nothing OrElse client.LastNull + interval > now Then Continue For
                            client.LastNull = now
                            msg.Invoke(New MessageContext(client.Client, Protocols.MessageDirection.ServerToClient))
                            client.Client.Send(msg.ToByteArray(client.Client.ProtocolType))
                        End SyncLock
                    Next
                End SyncLock

            Finally
                NullTimerLock = False
            End Try
        End Sub

        Private Shared Sub ProcessPingTimer(ByVal state As Object)
            If PingTimerLock Then Return
            PingTimerLock = True

            Try
                Dim stateL = TryCast(state, List(Of GameState))
                Dim msg = New SID_PING()
                Dim interval = TimeSpan.FromSeconds(180)
                Dim now = DateTime.Now
                Dim r = New Random()
                Dim clients As GameState()
                clients = stateL.ToArray()

                For Each client In clients

                    If client Is Nothing Then

                        SyncLock stateL
                            stateL.Remove(client)
                        End SyncLock

                        Continue For
                    End If

                    If client?.LastPing Is Nothing OrElse client.LastPing + interval > now Then Continue For
                    now = DateTime.Now
                    client.LastPing = now
                    client.PingDelta = now
                    client.PingToken = CUInt(r.[Next](0, &H7FFFFFFF))
                    msg.Invoke(New MessageContext(client.Client, Protocols.MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                        {"token", client.PingToken}
                    }))
                    client.Client.Send(msg.ToByteArray(client.Client.ProtocolType))
                Next

            Finally
                PingTimerLock = False
            End Try
        End Sub

        Public Shared Sub ScheduleShutdown(ByVal period As TimeSpan, ByVal Optional message As String = Nothing, ByVal Optional command As ChatCommandContext = Nothing)
            Dim rescheduled = False

            If ScheduledShutdown.EventTimer IsNot Nothing Then
                rescheduled = True
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, "Stopping previously scheduled shutdown timer")
                ScheduledShutdown.EventTimer.Dispose()
            End If

            ScheduledShutdown = New ShutdownEvent(message, False, DateTime.Now + period, New Timer(Sub(ByVal state As Object)
                                                                                                       Program.ExitCode = 0
                                                                                                       Program.[Exit] = True
                                                                                                   End Sub, command, period, period))
            Dim tsStr = $"{period.Hours} hour{(If(period.Hours = 1, "", "s"))} {period.Minutes} minute{(If(period.Minutes = 1, "", "s"))} {period.Seconds} second{(If(period.Seconds = 1, "", "s"))}"
            tsStr = tsStr.Replace("0 hours ", "")
            tsStr = tsStr.Replace("0 minutes ", "")
            tsStr = tsStr.Replace(" 0 seconds", "")
            Dim m As String

            If String.IsNullOrEmpty(message) AndAlso Not rescheduled Then
                m = Resources.ServerShutdownScheduled
            ElseIf String.IsNullOrEmpty(message) AndAlso rescheduled Then
                m = Resources.ServerShutdownRescheduled
            ElseIf Not String.IsNullOrEmpty(message) AndAlso Not rescheduled Then
                m = Resources.ServerShutdownScheduledWithMessage
            ElseIf Not String.IsNullOrEmpty(message) AndAlso rescheduled Then
                m = Resources.ServerShutdownRescheduledWithMessage
            Else
                Throw New InvalidOperationException("Cannot set server shutdown message from localized resource")
            End If

            m = m.Replace("{period}", tsStr)
            m = m.Replace("{message}", message)
            Logging.WriteLine(Logging.LogLevel.[Error], Logging.LogType.Server, m)
            Task.Run(Sub()
                         Dim lamChatEvent As New ChatEvent(ChatEvent.EventIds.EID_BROADCAST, Account.Flags.Admin, -1, "Battle.net", m)

                         SyncLock ActiveGameStates
                             For Each pair In ActiveGameStates
                                 lamChatEvent.WriteTo(pair.Value.Client)
                             Next
                         End SyncLock

                         If command IsNot Nothing Then
                             Dim r = Resources.AdminShutdownCommandScheduled

                             For Each kv In command.Environment
                                 r = r.Replace("{" & kv.Key & "}", kv.Value)
                             Next

                             For Each line In r.Split(Environment.NewLine)
                                 Call New ChatEvent(ChatEvent.EventIds.EID_INFO, CUInt(command.GameState.ChannelFlags), command.GameState.Ping, command.GameState.OnlineName, line).WriteTo(command.GameState.Client)
                             Next
                         End If
                     End Sub)
        End Sub

        Public Shared Sub ScheduleShutdownCancelled(ByVal Optional message As String = Nothing, ByVal Optional command As ChatCommandContext = Nothing)
            If ScheduledShutdown.Cancelled Then
                If command IsNot Nothing Then
                    Dim r = Resources.AdminShutdownCommandNotScheduled

                    For Each kv In command.Environment
                        r = r.Replace("{" & kv.Key & "}", kv.Value)
                    Next

                    For Each line In r.Split(Environment.NewLine)
                        Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, CUInt(command.GameState.ChannelFlags), command.GameState.Ping, command.GameState.OnlineName, line).WriteTo(command.GameState.Client)
                    Next
                End If
            Else
                Dim m = If(String.IsNullOrEmpty(message), Resources.ServerShutdownCancelled, Resources.ServerShutdownCancelledWithMessage)
                m = m.Replace("{message}", message)

                If ScheduledShutdown.EventTimer IsNot Nothing Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, "Stopping previously scheduled shutdown event")
                    ScheduledShutdown.EventTimer.Dispose()
                End If

                ScheduledShutdown = New ShutdownEvent(ScheduledShutdown.AdminMessage, True, ScheduledShutdown.EventDate, ScheduledShutdown.EventTimer)
                Logging.WriteLine(Logging.LogLevel.[Error], Logging.LogType.Server, m)
                Task.Run(Sub()
                             Dim lamChatEvent = New ChatEvent(ChatEvent.EventIds.EID_BROADCAST, Account.Flags.Admin, -1, "Battle.net", m)

                             SyncLock ActiveGameStates

                                 For Each pair In ActiveGameStates
                                     lamChatEvent.WriteTo(pair.Value.Client)
                                 Next
                             End SyncLock

                             If command IsNot Nothing Then
                                 Dim r = Resources.AdminShutdownCommandCancelled

                                 For Each kv In command.Environment
                                     r = r.Replace("{" & kv.Key & "}", kv.Value)
                                 Next

                                 For Each line In r.Split(Environment.NewLine)
                                     Call New ChatEvent(ChatEvent.EventIds.EID_INFO, CUInt(command.GameState.ChannelFlags), command.GameState.Ping, command.GameState.OnlineName, line).WriteTo(command.GameState.Client)
                                 Next
                             End If
                         End Sub)
            End If
        End Sub
    End Class
End Namespace
