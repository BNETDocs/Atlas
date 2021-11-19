Imports AtlasV.Battlenet.Protocols.Game
Imports AtlasV.Battlenet.Protocols.Game.Messages
Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Net
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Namespace AtlasV.Battlenet
    Class Channel
        Public Const TheVoidFlags As Flags = Flags.[Public] Or Flags.Silent

        <Flags>
        Public Enum Flags As UInt32
            None = &H0
            [Public] = &H1
            Moderated = &H2
            Restricted = &H4
            Silent = &H8
            System = &H10
            ProductSpecific = &H20
            [Global] = &H1000
            Redirected = &H4000
            Chat = &H8000
            TechSupport = &H10000
        End Enum

        Public Property ActiveFlags As Flags
        Public Property AllowNewUsers As Boolean
        Protected Property BannedUsers As List(Of GameState)

        Public ReadOnly Property Count As Integer
            Get
                Return Users.Count
            End Get
        End Property

        Public Property DesignatedHeirs As Dictionary(Of GameState, GameState)
        Public Property MaxUsers As Integer
        Public Property Name As String
        Public Property Topic As String
        Protected Property Users As ConcurrentBag(Of GameState)

        Private Sub New(ByVal varName As String,
                        ByVal Optional varFlags As Flags = Flags.None,
                        ByVal Optional varMaxUsers As Integer = -1,
                        ByVal Optional varTopic As String = "")
            ActiveFlags = varFlags
            AllowNewUsers = True
            BannedUsers = New List(Of GameState)()
            DesignatedHeirs = New Dictionary(Of GameState, GameState)()
            MaxUsers = varMaxUsers
            Name = varName
            Topic = varTopic
            Users = New ConcurrentBag(Of GameState)()
        End Sub

        Public Sub AcceptUser(ByVal user As GameState, ByVal Optional ignoreLimits As Boolean = False, ByVal Optional extendedErrors As Boolean = False)
            If Not ignoreLimits Then
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Channel, $"[{Name}] Evaluating limits for user [{user.OnlineName}]")

                If MaxUsers > -1 AndAlso Users.Count >= MaxUsers Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"[{Name}] Rejecting user [{user.OnlineName}] for reason [Full]")

                    If extendedErrors Then
                        Call New ChatEvent(ChatEvent.EventIds.EID_CHANNELFULL, ActiveFlags, 0, "", Name).WriteTo(user.Client)
                    Else
                        Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.ChannelIsFull).WriteTo(user.Client)
                    End If

                    Return
                End If

                SyncLock BannedUsers

                    If BannedUsers.Contains(user) Then
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"[{Name}] Rejecting user [{user.OnlineName}] for reason [Banned]")

                        If extendedErrors Then
                            Call New ChatEvent(ChatEvent.EventIds.EID_CHANNELRESTRICTED, ActiveFlags, 0, "", Name).WriteTo(user.Client)
                        Else
                            Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.YouAreBannedFromThatChannel).WriteTo(user.Client)
                        End If

                        Return
                    End If
                End SyncLock

                If ActiveFlags.HasFlag(Flags.Restricted) OrElse ActiveFlags.HasFlag(Flags.System) Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"[{Name}] Rejecting user [{user.OnlineName}] for reason [Restricted]")

                    If extendedErrors Then
                        Call New ChatEvent(ChatEvent.EventIds.EID_CHANNELRESTRICTED, ActiveFlags, 0, "", Name).WriteTo(user.Client)
                    Else
                        Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.ChannelIsRestricted).WriteTo(user.Client)
                    End If

                    Return
                End If
            End If

            If user.ActiveChannel IsNot Nothing Then user.ActiveChannel.RemoveUser(user)
            user.ActiveChannel = Me
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, String.Format("[{0}] Accepting user [{1}] (ignoreLimits: {2})", Name, user.OnlineName, ignoreLimits))

            SyncLock Users
                Users.Add(user)
            End SyncLock

            Call New ChatEvent(ChatEvent.EventIds.EID_CHANNELJOIN, ActiveFlags, 0, "", Name).WriteTo(user.Client)

            If Not ActiveFlags.HasFlag(Flags.Silent) Then
                Dim rereUsers As GameState()

                SyncLock Users
                    rereUsers = Users.ToArray()
                End SyncLock

                SyncLock user

                    For Each subuser In rereUsers
                        Call New ChatEvent(ChatEvent.EventIds.EID_USERSHOW, RenderChannelFlags(user, subuser), subuser.Ping, RenderOnlineName(user, subuser), subuser.Statstring).WriteTo(user.Client)

                        If Object.Equals(subuser, user) = False Then
                            Call New ChatEvent(ChatEvent.EventIds.EID_USERJOIN, RenderChannelFlags(subuser, user), user.Ping, RenderOnlineName(subuser, user), user.Statstring).WriteTo(subuser.Client)
                        End If
                    Next
                End SyncLock
            Else
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, Resources.ChannelIsChatRestricted).WriteTo(user.Client)
            End If

            Dim topic = RenderTopic(user).Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(vbLf)

            For Each line In topic
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line).WriteTo(user.Client)
            Next

            If Common.ScheduledShutdown.EventDate > DateTime.Now Then
                Dim ts = Common.ScheduledShutdown.EventDate - DateTime.Now
                Dim tsStr = $"{ts.Hours} hour{(If(ts.Hours = 1, "", "s"))} {ts.Minutes} minute{(If(ts.Minutes = 1, "", "s"))} {ts.Seconds} second{(If(ts.Seconds = 1, "", "s"))}"
                tsStr = tsStr.Replace("0 hours ", "")
                tsStr = tsStr.Replace("0 minutes ", "")
                tsStr = tsStr.Replace(" 0 seconds", "")
                Dim m = If(String.IsNullOrEmpty(Common.ScheduledShutdown.AdminMessage), Resources.ServerShutdownScheduled, Resources.ServerShutdownScheduledWithMessage)
                m = m.Replace("{message}", Common.ScheduledShutdown.AdminMessage)
                m = m.Replace("{period}", tsStr)
                Call New ChatEvent(ChatEvent.EventIds.EID_BROADCAST, Account.Flags.Admin, -1, "Battle.net", m).WriteTo(user.Client)
            End If

            Dim autoOp = Settings.GetBoolean(New String() {"channel", "auto_op"}, False)
            If (autoOp = True AndAlso Count = 1 AndAlso IsPrivate()) OrElse Name.ToLower() = "op " & user.OnlineName.ToLower() Then UpdateUser(user, user.ChannelFlags Or Account.Flags.ChannelOp)
        End Sub

        Public Sub BanUser(ByVal source As GameState, ByVal target As String, ByVal reason As String)
            Dim targetClient = Nothing

            If Not Common.GetClientByOnlineName(target, targetClient) OrElse targetClient Is Nothing Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.InvalidUser).WriteTo(source.Client)
                Return
            End If

            BanUser(source, targetClient, reason)
        End Sub

        Public Sub BanUser(ByVal source As GameState, ByVal target As GameState, ByVal reason As String)
            If target Is Nothing Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.InvalidUser).WriteTo(source.Client)
                Return
            End If

            Dim sourceSudoPrivs = source.ChannelFlags.HasFlag(Account.Flags.Admin) OrElse source.ChannelFlags.HasFlag(Account.Flags.Employee)
            Dim targetSudoPrivs = target.ChannelFlags.HasFlag(Account.Flags.Admin) OrElse target.ChannelFlags.HasFlag(Account.Flags.ChannelOp) OrElse target.ChannelFlags.HasFlag(Account.Flags.Employee)

            If targetSudoPrivs AndAlso Not sourceSudoPrivs Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.YouCannotBanAChannelOperator).WriteTo(source.Client)
                Return
            End If

            SyncLock BannedUsers

                If BannedUsers.Contains(target) Then
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.UserIsAlreadyBanned.Replace("{target}", target.OnlineName)).WriteTo(source.Client)
                    Return
                End If

                BannedUsers.Add(target)
            End SyncLock

            Dim sourceName = source.OnlineName
            Dim maskAdminsInBanMessage = Settings.GetBoolean(New String() {"battlenet", "emulation", "mask_admins_in_ban_message"}, False)

            If maskAdminsInBanMessage AndAlso (source.ChannelFlags.HasFlag(Account.Flags.Employee) OrElse source.ChannelFlags.HasFlag(Account.Flags.Admin)) Then
                sourceName = $"a {Resources.BattlenetRepresentative}"
            End If

            Dim bannedStr = If(String.IsNullOrEmpty(reason), Resources.UserBannedFromChannel, Resources.UserBannedFromChannelWithReason)
            bannedStr = bannedStr.Replace("{reason}", reason)
            bannedStr = bannedStr.Replace("{source}", sourceName)
            bannedStr = bannedStr.Replace("{target}", target.OnlineName)
            WriteChatEvent(New ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, bannedStr))
            Dim rereUsers = New List(Of GameState)(Users)

            If rereUsers.Contains(target) Then
                RemoveUser(target)
                bannedStr = Resources.YouWereBannedFromChannel
                bannedStr = bannedStr.Replace("{reason}", reason)
                bannedStr = bannedStr.Replace("{source}", sourceName)
                bannedStr = bannedStr.Replace("{target}", target.OnlineName)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, bannedStr).WriteTo(target.Client)
                Dim theVoid = GetChannelByName(Resources.TheVoid, True)
                MoveUser(target, theVoid, True)
            End If
        End Sub

        Public Sub Designate(ByVal designator As GameState, ByVal heir As GameState)
            DesignatedHeirs(designator) = heir
        End Sub

        Public Sub Dispose()
            BannedUsers = Nothing
            DesignatedHeirs = Nothing

            If Users IsNot Nothing Then
                Dim theVoid = GetChannelByName(Resources.TheVoid, True)

                For Each user In Users
                    MoveUser(user, theVoid, True)
                Next
            End If

            SyncLock Common.ActiveChannels
                If Common.ActiveChannels.ContainsKey(Name) Then Common.ActiveChannels.Remove(Name)
            End SyncLock
        End Sub

        Public Shared Function GetChannelByName(ByVal name As String, ByVal autoCreate As Boolean) As Channel
            Dim channel As Channel = Nothing
            If String.IsNullOrEmpty(name) Then Return channel
            If name(0) = "#"c Then name = name.Substring(1)

            SyncLock Common.ActiveChannels
                Common.ActiveChannels.TryGetValue(name, channel)
            End SyncLock

            If channel IsNot Nothing OrElse Not autoCreate Then Return channel
            Dim staticName As String = Nothing, staticFlags As Flags = Nothing, staticMaxUsers As Integer = Nothing, staticTopic As String = Nothing, staticProducts() As Product.ProductCode = Nothing
            Dim isStatic = GetStaticChannel(name, staticName, staticFlags, staticMaxUsers, staticTopic, staticProducts)

            If Not isStatic Then
                channel = New Channel(name, Flags.None)
            Else
                channel = New Channel(staticName, staticFlags, staticMaxUsers, staticTopic)
            End If

            SyncLock Common.ActiveChannels
                Common.ActiveChannels.Add(channel.Name, channel)
            End SyncLock

            Return channel
        End Function

        Public Shared Function GetStaticChannel(ByVal search As String, <Out> ByRef name As String, <Out> ByRef flags As Flags, <Out> ByRef maxUsers As Integer, <Out> ByRef topic As String, <Out> ByRef products As Product.ProductCode()) As Boolean
            Dim searchL = search.ToLower()
            Dim channelJson = Nothing
            Settings.State.RootElement.TryGetProperty("channel", channelJson)
            Dim staticJson = Nothing
            channelJson.TryGetProperty("static", staticJson)
            Dim chNameJson = Nothing, chFlagsJson = Nothing, chMaxUsersJson = Nothing, chTopicJson = Nothing, chProductsJson = Nothing

            For Each ch In staticJson.EnumerateArray()
                Dim hasName = ch.TryGetProperty("name", chNameJson)
                Dim chName = If(Not hasName, Nothing, chNameJson.GetString())

                If chName Is Nothing OrElse chName.ToLower() <> searchL Then
                    Continue For
                End If

                Dim hasFlags = ch.TryGetProperty("flags", chFlagsJson)
                Dim hasMaxUsers = ch.TryGetProperty("max_users", chMaxUsersJson)
                Dim hasTopic = ch.TryGetProperty("topic", chTopicJson)
                Dim hasProducts = ch.TryGetProperty("products", chProductsJson)
                Dim chFlags = If(Not hasFlags, Flags.None, CType(chFlagsJson.GetUInt32(), Flags))
                Dim chMaxUsers = If(Not hasMaxUsers, -1, chMaxUsersJson.GetInt32())
                Dim chTopic = If(Not hasTopic, "", chTopicJson.GetString())
                Dim chProducts As Product.ProductCode() = Nothing

                If hasProducts Then
                    Dim _list = New List(Of Product.ProductCode)()

                    For Each productJson In chProductsJson.EnumerateArray()
                        Dim productStr = productJson.GetString()
                        _list.Add(Product.StringToProduct(productStr))
                    Next

                    chProducts = _list.ToArray()
                End If

                name = chName
                flags = chFlags
                maxUsers = chMaxUsers
                topic = chTopic
                products = chProducts
                Return True
            Next

            name = Nothing
            flags = 0
            maxUsers = -1
            topic = Nothing
            products = Array.Empty(Of Product.ProductCode)()
            Return False
        End Function

        Public Function GetUsersAsString(ByVal context As GameState) As String
            If ActiveFlags.HasFlag(Flags.Silent) Then Return String.Empty
            Dim names = New LinkedList(Of String)()

            SyncLock Users

                For Each user In Users
                    Dim userName = RenderOnlineName(context, user)

                    If user.ChannelFlags.HasFlag(Account.Flags.Employee) OrElse user.ChannelFlags.HasFlag(Account.Flags.ChannelOp) OrElse user.ChannelFlags.HasFlag(Account.Flags.Admin) Then
                        names.AddFirst($"[{userName.ToUpper()}]")
                    Else
                        names.AddLast(userName)
                    End If
                Next
            End SyncLock

            Dim s = ""
            Dim i = 0

            For Each n In names

                If i Mod 2 = 0 Then
                    s += $"{n}, "
                Else
                    s += $"{n}{Battlenet.Common.NewLine}"
                End If

                i += 1
            Next

            If i Mod 2 <> 0 Then s = s.Substring(0, s.Length - 2) 's(0.. Xor 2) 
            Return s
        End Function

        Public Function IsPrivate() As Boolean
            Return Not ActiveFlags.HasFlag(Flags.[Public])
        End Function

        Public Function IsPublic() As Boolean
            Return ActiveFlags.HasFlag(Flags.[Public])
        End Function

        Public Sub KickUser(ByVal source As GameState, ByVal target As String, ByVal reason As String)
            Dim targetClient As GameState = Nothing

            SyncLock Users

                For Each client In Users

                    If client.OnlineName = target Then
                        targetClient = client
                        Exit For
                    End If
                Next
            End SyncLock

            If targetClient Is Nothing Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.InvalidUser).WriteTo(source.Client)
                Return
            End If

            Dim sourceSudoPrivs = source.ChannelFlags.HasFlag(Account.Flags.Admin) OrElse source.ChannelFlags.HasFlag(Account.Flags.Employee)
            Dim targetSudoPrivs = targetClient.ChannelFlags.HasFlag(Account.Flags.Admin) OrElse targetClient.ChannelFlags.HasFlag(Account.Flags.ChannelOp) OrElse targetClient.ChannelFlags.HasFlag(Account.Flags.Employee)

            If targetSudoPrivs AndAlso Not sourceSudoPrivs Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.YouCannotKickAChannelOperator).WriteTo(source.Client)
                Return
            End If

            Dim sourceName = source.OnlineName
            Dim maskAdminsInKickMessage = Settings.GetBoolean(New String() {"battlenet", "emulation", "mask_admins_in_kick_message"}, False)

            If maskAdminsInKickMessage AndAlso (source.ChannelFlags.HasFlag(Account.Flags.Employee) OrElse source.ChannelFlags.HasFlag(Account.Flags.Admin)) Then
                sourceName = $"a {Resources.BattlenetRepresentative}"
            End If

            Dim kickedStr = If(reason.Length > 0, Resources.UserKickedFromChannelWithReason, Resources.UserKickedFromChannel)
            kickedStr = kickedStr.Replace("{reason}", reason)
            kickedStr = kickedStr.Replace("{source}", sourceName)
            kickedStr = kickedStr.Replace("{target}", target)
            WriteChatEvent(New ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, kickedStr))
            RemoveUser(targetClient)
            kickedStr = Resources.YouWereKickedFromChannel
            kickedStr = kickedStr.Replace("{reason}", reason)
            kickedStr = kickedStr.Replace("{source}", sourceName)
            kickedStr = kickedStr.Replace("{target}", target)
            Call New ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, kickedStr).WriteTo(targetClient.Client)
            Dim theVoid = GetChannelByName(Resources.TheVoid, True)
            MoveUser(targetClient, theVoid, True)
        End Sub

        Public Shared Sub MoveUser(ByVal client As GameState, ByVal name As String, ByVal Optional ignoreLimits As Boolean = True)
            Dim channel = GetChannelByName(name, True)
            MoveUser(client, channel, ignoreLimits)
        End Sub

        Public Shared Sub MoveUser(ByVal client As GameState, ByVal channel As Channel, ByVal Optional ignoreLimits As Boolean = True)
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Channel, $"Moving user [{client.OnlineName}] {(If(client.ActiveChannel IsNot Nothing, $"from [{client.ActiveChannel.Name}] ", ""))}to [{channel.Name}] (ignoreLimits: {ignoreLimits})")
            channel.AcceptUser(client, ignoreLimits)
        End Sub

        Public Sub RemoveUser(ByVal user As GameState)
            Dim notify As Boolean = False
            Dim rereUsers As List(Of GameState)

            SyncLock Users
                rereUsers = New List(Of GameState)(Users)

                If rereUsers.Contains(user) Then
                    rereUsers.Remove(user)
                    Users = New ConcurrentBag(Of GameState)(rereUsers)
                    notify = True
                End If
            End SyncLock

            If Not notify Then
                If Count = 0 AndAlso Not ActiveFlags.HasFlag(Flags.[Public]) Then Dispose()
                Return
            End If

            SyncLock user
                Dim remoteAddress = IPAddress.Parse(user.Client.RemoteEndPoint.ToString().Split(":"c)(0))
                Dim squelched = user.SquelchedIPs.Contains(remoteAddress)
                Dim rereFlags = If(squelched, user.ChannelFlags Or Account.Flags.Squelched, user.ChannelFlags And Not Account.Flags.Squelched)
                user.ActiveChannel = Nothing
                Dim wasChannelOp = user.ChannelFlags.HasFlag(Account.Flags.Employee) OrElse user.ChannelFlags.HasFlag(Account.Flags.ChannelOp) OrElse user.ChannelFlags.HasFlag(Account.Flags.Admin)
                user.ChannelFlags = user.ChannelFlags And Not Account.Flags.ChannelOp

                If Not ActiveFlags.HasFlag(Flags.Silent) Then
                    Dim emptyStatstring = Array.Empty(Of Byte)()

                    For Each subuser In Users
                        Call New ChatEvent(ChatEvent.EventIds.EID_USERLEAVE, RenderChannelFlags(subuser, user), user.Ping, RenderOnlineName(subuser, user), emptyStatstring).WriteTo(subuser.Client)
                    Next
                End If

                SyncLock DesignatedHeirs
                    Dim designatedHeirExists = DesignatedHeirs.ContainsKey(user)

                    If wasChannelOp AndAlso designatedHeirExists AndAlso users.Contains(DesignatedHeirs(user)) Then
                        Dim heir = DesignatedHeirs(user)

                        If heir IsNot Nothing AndAlso Object.Equals(heir.ActiveChannel, Me) = True AndAlso Not heir.ChannelFlags.HasFlag(Account.Flags.ChannelOp) Then
                            UpdateUser(heir, heir.ChannelFlags Or Account.Flags.ChannelOp)
                        End If
                    End If

                    If designatedHeirExists Then
                        DesignatedHeirs.Remove(user)
                    End If
                End SyncLock
            End SyncLock

            If Count = 0 AndAlso Not ActiveFlags.HasFlag(Flags.[Public]) Then Dispose()
        End Sub

        Public Shared Function RenderChannelFlags(ByVal context As GameState, ByVal target As GameState) As Account.Flags
            Dim targetFlags = target.ChannelFlags
            Dim targetRemoteAddress = IPAddress.Parse(target.Client.RemoteEndPoint.ToString().Split(":"c)(0))
            Dim targetSquelched = context.SquelchedIPs.Contains(targetRemoteAddress)
            targetFlags = If(targetSquelched, targetFlags Or Account.Flags.Squelched, targetFlags And Not Account.Flags.Squelched)
            Return targetFlags
        End Function

        Public Shared Function RenderOnlineName(ByVal context As GameState, ByVal target As GameState) As String
            Dim targetName = target.OnlineName

            If Product.IsDiabloII(context.Product) Then
                targetName = $"{Encoding.UTF8.GetString(target.CharacterName)}*{targetName}"
            End If

            Return targetName
        End Function

        Public Function RenderTopic(ByVal receiver As GameState) As String
            Dim r = Topic
            r = r.Replace("{account}", CStr(receiver.ActiveAccount.[Get](Account.UsernameKey)))
            r = r.Replace("{channel}", Name)
            r = r.Replace("{channelMaxUsers}", MaxUsers.ToString())
            r = r.Replace("{channelUserCount}", Users.Count.ToString())
            r = r.Replace("{game}", Product.ProductName(receiver.Product, False))
            r = r.Replace("{gameFull}", Product.ProductName(receiver.Product, True))
            r = r.Replace("{ping}", receiver.Ping.ToString() & "ms")
            r = r.Replace("{user}", receiver.OnlineName)
            r = r.Replace("{username}", receiver.OnlineName)
            r = r.Replace("{userName}", receiver.OnlineName)
            r = r.Replace("{userPing}", receiver.Ping.ToString() & "ms")
            Return r
        End Function

        Public Sub Resync()
            Dim args = New Dictionary(Of String, Object) From {
                {"chatEvent", Nothing}
            }
            Dim msg = New SID_CHATEVENT()

            SyncLock Users

                For Each user In Users
                    args("chatEvent") = New ChatEvent(ChatEvent.EventIds.EID_CHANNELJOIN, ActiveFlags, 0, "", Name)
                    msg.Invoke(New MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args))
                    user.Client.Send(msg.ToByteArray(user.Client.ProtocolType))

                    If Not ActiveFlags.HasFlag(Flags.Silent) Then

                        For Each subuser In Users
                            args("chatEvent") = New ChatEvent(ChatEvent.EventIds.EID_USERSHOW, RenderChannelFlags(user, subuser), subuser.Ping, RenderOnlineName(user, subuser), subuser.Statstring)
                            msg.Invoke(New MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args))
                            user.Client.Send(msg.ToByteArray(user.Client.ProtocolType))
                        Next
                    Else
                        args("chatEvent") = New ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, Resources.ChannelIsChatRestricted)
                        msg.Invoke(New MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args))
                        user.Client.Send(msg.ToByteArray(user.Client.ProtocolType))
                    End If

                    Dim topic = RenderTopic(user)

                    For Each line In topic.Split(vbLf)
                        args("chatEvent") = New ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line)
                        msg.Invoke(New MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args))
                        user.Client.Send(msg.ToByteArray(user.Client.ProtocolType))
                    Next
                Next
            End SyncLock
        End Sub

        Public Sub SetActiveFlags(ByVal newFlags As Flags)
            ActiveFlags = newFlags
            Resync()
        End Sub

        Public Sub SetAllowNewUsers(ByVal allowNewUsers As Boolean)
            allowNewUsers = allowNewUsers
            Dim r = If(allowNewUsers, Resources.ChannelIsNowPublic, Resources.ChannelIsNowPrivate)
            WriteChatEvent(New ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, r))
        End Sub

        Public Sub SetMaxUsers(ByVal maxUsers As Integer)
            maxUsers = maxUsers
            Dim r = Resources.ChannelMaxUsersChanged.Replace("{maxUsers}", $"{maxUsers}")
            WriteChatEvent(New ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, r))
        End Sub

        Public Sub SetName(ByVal newName As String)
            Dim oldName = Name
            Name = newName
            If Users.IsEmpty Then Return '.Count = 0
            Dim r = Resources.ChannelWasRenamed.Replace("{oldName}", oldName).Replace("{newName}", newName)
            WriteChatEvent(New ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, r))
            Resync()
        End Sub

        Public Sub SetTopic(ByVal newTopic As String)
            Topic = newTopic
            WriteChatEvent(New ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, Resources.ChannelTopicChanged))

            SyncLock Users

                For Each user In Users
                    Dim lines = RenderTopic(user).Split(vbLf)

                    For Each line In lines
                        Call New ChatEvent(ChatEvent.EventIds.EID_INFO, ActiveFlags, 0, Name, line).WriteTo(user.Client)
                    Next
                Next
            End SyncLock
        End Sub

        Public Sub SquelchUpdate(ByVal client As GameState)
            If client Is Nothing Then Throw New NullReferenceException("Client parameter must not be null")

            SyncLock Users

                For Each user In Users
                    Call New ChatEvent(ChatEvent.EventIds.EID_USERUPDATE, RenderChannelFlags(client, user), user.Ping, RenderOnlineName(client, user), user.Statstring).WriteTo(client.Client)
                Next
            End SyncLock
        End Sub

        Public Sub UnBanUser(ByVal source As GameState, ByVal target As String)
            Dim targetClient = Nothing

            If Not Common.GetClientByOnlineName(target, targetClient) OrElse targetClient Is Nothing Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.UserNotLoggedOn).WriteTo(source.Client)
                Return
            End If

            UnBanUser(source, targetClient)
        End Sub

        Public Sub UnBanUser(ByVal source As GameState, ByVal target As GameState)
            If target Is Nothing Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.InvalidUser).WriteTo(source.Client)
                Return
            End If

            Dim wasBanned = False

            SyncLock BannedUsers

                If BannedUsers.Contains(target) Then
                    BannedUsers.Remove(target)
                    wasBanned = True
                End If
            End SyncLock

            If Not wasBanned Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ActiveFlags, 0, Name, Resources.UserIsNotBanned.Replace("{target}", target.OnlineName)).WriteTo(source.Client)
                Return
            End If

            Dim sourceName = source.OnlineName
            Dim maskAdminsInBanMessage = Settings.GetBoolean(New String() {"battlenet", "emulation", "mask_admins_in_ban_message"}, False)

            If maskAdminsInBanMessage AndAlso (source.ChannelFlags.HasFlag(Account.Flags.Employee) OrElse source.ChannelFlags.HasFlag(Account.Flags.Admin)) Then
                sourceName = $"a {Resources.BattlenetRepresentative}"
            End If

            Dim bannedStr = Resources.UserUnBannedFromChannel
            bannedStr = bannedStr.Replace("{source}", sourceName)
            bannedStr = bannedStr.Replace("{target}", target.OnlineName)
            WriteChatEvent(New ChatEvent(ChatEvent.EventIds.EID_INFO, source.ChannelFlags, source.Ping, source.OnlineName, bannedStr))
        End Sub

        Public Sub UpdateUser(ByVal client As GameState)
            UpdateUser(client, client.ChannelFlags, client.Ping, client.Statstring)
        End Sub

        Public Sub UpdateUser(ByVal client As GameState, ByVal flags As Account.Flags)
            UpdateUser(client, flags, client.Ping, client.Statstring)
        End Sub

        Public Sub UpdateUser(ByVal client As GameState, ByVal ping As Int32)
            UpdateUser(client, client.ChannelFlags, ping, client.Statstring)
        End Sub

        Public Sub UpdateUser(ByVal client As GameState, ByVal statstring As Byte())
            UpdateUser(client, client.ChannelFlags, client.Ping, statstring)
        End Sub

        Public Sub UpdateUser(ByVal client As GameState, ByVal statstring As String)
            UpdateUser(client, client.ChannelFlags, client.Ping, statstring)
        End Sub

        Public Sub UpdateUser(ByVal client As GameState, ByVal flags As Account.Flags, ByVal ping As Int32, ByVal statstring As Byte(), ByVal Optional forceEvent As Boolean = False)
            Dim changed = False

            If client.ChannelFlags <> flags Then
                client.ChannelFlags = flags
                changed = True
            End If

            If client.Ping <> ping Then
                client.Ping = ping
                changed = True
            End If

            'CHECK THIS SHIT
            If client.Statstring.SequenceEqual(statstring) = False Then
                client.Statstring = statstring
                changed = True
            End If

            If Not changed AndAlso Not forceEvent Then Return

            SyncLock Users

                For Each user In Users
                    Call New ChatEvent(ChatEvent.EventIds.EID_USERUPDATE, RenderChannelFlags(user, client), client.Ping, RenderOnlineName(user, client), client.Statstring).WriteTo(user.Client)
                Next
            End SyncLock
        End Sub

        Public Sub UpdateUser(ByVal client As GameState, ByVal flags As Account.Flags, ByVal ping As Int32, ByVal statstring As String)
            UpdateUser(client, flags, ping, Encoding.UTF8.GetBytes(statstring))
        End Sub

        Public Sub WriteChatEvent(ByVal chatEvent As ChatEvent, ByVal Optional owner As GameState = Nothing)
            Dim args = New Dictionary(Of String, Object) From {
                {"chatEvent", chatEvent}
            }
            Dim msg = New SID_CHATEVENT()

            SyncLock Users

                For Each user In Users

                    If owner IsNot Nothing AndAlso Object.Equals(user, owner) = True AndAlso chatEvent.EventId = ChatEvent.EventIds.EID_TALK Then
                        Continue For
                    End If

                    msg.Invoke(New MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, args))
                    user.Client.Send(msg.ToByteArray(user.Client.ProtocolType))
                Next
            End SyncLock
        End Sub

        Public Sub WriteChatMessage(ByVal owner As GameState, ByVal message As Byte(), ByVal Optional emote As Boolean = False)
            Dim msg = New SID_CHATEVENT()

            SyncLock Users

                For Each user In Users

                    If owner IsNot Nothing AndAlso Object.Equals(user, owner) = True AndAlso Not emote Then
                        Continue For
                    End If

                    Dim e = New ChatEvent(If(emote, ChatEvent.EventIds.EID_EMOTE, ChatEvent.EventIds.EID_TALK), RenderChannelFlags(user, owner), owner.Ping, RenderOnlineName(user, owner), message)
                    msg.Invoke(New MessageContext(user.Client, Protocols.MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                        {"chatEvent", e}
                    }))
                    user.Client.Send(msg.ToByteArray(user.Client.ProtocolType))
                Next
            End SyncLock
        End Sub
    End Class
End Namespace