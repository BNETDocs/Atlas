Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Namespace AtlasV.Battlenet.Protocols.Game
    Class GameState
        Implements IDisposable

        Public Enum LogonTypes As UInt32
            OLS = 0
            NLSBeta = 1
            NLS = 2
        End Enum

        Private IsDisposing As Boolean = False
        Public Property Client As ClientState
        Public ActiveAccount As Account
        Public ActiveChannel As Channel
        Public ActiveClan As Clan
        Public ChannelFlags As Account.Flags
        Public ConnectedTimestamp As DateTime
        Public GameAd As GameAd
        Public GameDataAddress As IPAddress
        Public GameKeys As List(Of GameKey)
        Public LastLogon As DateTime
        Public LastNull As DateTime
        Public LastPing As DateTime
        Public LocalIPAddress As IPAddress

        Public ReadOnly Property LocalTime As DateTime
            Get
                Return DateTime.UtcNow.AddMinutes(0 - TimezoneBias)
            End Get
        End Property

        Public Locale As LocaleInfo
        Public LogonType As LogonTypes
        Public SquelchedIPs As List(Of IPAddress)
        Public PingDelta As DateTime
        Public Platform As Platform.PlatformCode
        Public Product As Product.ProductCode
        Public Version As VersionInfo
        Public Away As String
        Public CharacterName As Byte()
        Public ClientToken As UInt32
        Public DoNotDisturb As String
        Public FailedLogons As UInt32
        Public GameDataPort As UShort
        Public KeyOwner As Byte()
        Public OnlineName As String
        Public Ping As Int32
        Public PingToken As UInt32
        Public ProtocolId As UInt32
        Public ServerToken As UInt32
        Public SpawnKey As Boolean
        Public Statstring As Byte()
        Public TimezoneBias As Int32
        Public UDPSupported As Boolean
        Public UDPToken As UInt32
        Public Username As String

        Public Sub New(ByVal varClient As ClientState)
            Dim r = New Random()
            Client = varClient
            ActiveAccount = Nothing
            ActiveChannel = Nothing
            ActiveClan = Nothing
            ChannelFlags = Account.Flags.None
            ConnectedTimestamp = DateTime.Now
            GameAd = Nothing
            GameDataAddress = Nothing
            GameKeys = New List(Of GameKey)()
            LastLogon = DateTime.Now
            LastNull = DateTime.Now
            LastPing = DateTime.MinValue
            LocalIPAddress = Nothing
            Locale = New LocaleInfo()
            LogonType = LogonTypes.OLS
            SquelchedIPs = New List(Of IPAddress)()
            PingDelta = LastPing
            Platform = Battlenet.Platform.PlatformCode.None
            Product = Battlenet.Product.ProductCode.None
            Version = New VersionInfo()
            Away = Nothing
            DoNotDisturb = Nothing
            CharacterName = Array.Empty(Of Byte)()
            ClientToken = 0
            FailedLogons = 0
            GameDataPort = 0
            KeyOwner = Nothing
            OnlineName = Nothing
            Ping = -1
            PingToken = CUInt(r.[Next](0, &H7FFFFFFF))
            ProtocolId = 0
            ServerToken = CUInt(r.[Next](0, &H7FFFFFFF))
            Statstring = Nothing
            SpawnKey = False
            TimezoneBias = 0
            UDPSupported = False
            UDPToken = CUInt(r.[Next](0, &H7FFFFFFF))
            Username = Nothing
            Task.Run(Sub()

                         SyncLock Battlenet.Common.NullTimerState
                             Battlenet.Common.NullTimerState.Add(Me)
                         End SyncLock
                     End Sub)
            Task.Run(Sub()

                         SyncLock Battlenet.Common.PingTimerState
                             Battlenet.Common.PingTimerState.Add(Me)
                         End SyncLock
                     End Sub)
        End Sub

        Public Sub Close()
            If ActiveChannel IsNot Nothing Then
                ActiveChannel.RemoveUser(Me)
            End If

            If ActiveClan IsNot Nothing Then
                ActiveClan.WriteStatusChange(Me, False)
            End If

            If OnlineName IsNot Nothing Then
                SyncLock Battlenet.Common.ActiveGameStates
                    If Battlenet.Common.ActiveGameStates.ContainsKey(OnlineName.ToLower()) Then
                        Battlenet.Common.ActiveGameStates.Remove(OnlineName.ToLower())
                    End If
                End SyncLock
            End If

            If ActiveAccount IsNot Nothing Then

                SyncLock ActiveAccount
                    ActiveAccount.[Set](Account.LastLogoffKey, DateTime.Now)
                    Dim timeLogged = CUInt(ActiveAccount.[Get](Account.TimeLoggedKey))
                    Dim diff = DateTime.Now - ConnectedTimestamp
                    timeLogged += CUInt(Math.Round(diff.TotalSeconds))
                    ActiveAccount.[Set](Account.TimeLoggedKey, timeLogged)
                    Dim username = CStr(ActiveAccount.[Get](Account.UsernameKey))
                    If Battlenet.Common.ActiveAccounts.ContainsKey(OnlineName.ToLower()) Then
                        Battlenet.Common.ActiveAccounts.Remove(OnlineName.ToLower())
                    End If
                End SyncLock
            End If

            StopGameAd()

            SyncLock Battlenet.Common.NullTimerState
                Battlenet.Common.NullTimerState.Remove(Me)
            End SyncLock

            SyncLock Battlenet.Common.PingTimerState
                Battlenet.Common.PingTimerState.Remove(Me)
            End SyncLock
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If IsDisposing Then Return
            IsDisposing = True
            Close()
            IsDisposing = False
        End Sub

        Public Sub GenerateStatstring()
            Dim buf As Byte() = Nothing
            Dim m As MemoryStream = Nothing
            Dim w As BinaryWriter = Nothing

            Try
                buf = New Byte(127) {}
                m = New MemoryStream(buf)
                w = New BinaryWriter(m)
                w.Write(CUInt(Product))
                Dim rereProduct = New Byte(3) {}
                Buffer.BlockCopy(buf, 0, rereProduct, 0, rereProduct.Length)
                Dim game = Encoding.UTF8.GetString(rereProduct)

                If Product <> Battlenet.Product.ProductCode.StarcraftShareware AndAlso (Battlenet.Product.IsStarcraft(Product) OrElse Battlenet.Product.IsWarcraftII(Product)) Then
                    Dim ladderRating = CUInt(0)
                    Dim ladderRank = CUInt(0)
                    Dim wins = CUInt(ActiveAccount.[Get]($"record\\{game}\\0\\wins", 0,))
                    Dim leagueId = CUInt(ActiveAccount.[Get]("System\League", 0,))
                    Dim highLadderRating = CUInt(ActiveAccount.[Get]($"record\\{game}\\1\\rating", 0,))
                    Dim ironManLadderRating = CUInt(ActiveAccount.[Get]($"record\\{game}\\3\\rating", 0,))
                    Dim ironManLadderRank = CUInt(ActiveAccount.[Get]($"record\\{game}\\3\\rank", 0,))
                    Dim iconCode = CType(ActiveAccount.[Get]("System\Icon", Product), Byte())
                    w.Write(" "c)
                    w.Write(Encoding.UTF8.GetBytes(ladderRating.ToString()))
                    w.Write(" "c)
                    w.Write(Encoding.UTF8.GetBytes(ladderRank.ToString()))
                    w.Write(" "c)
                    w.Write(Encoding.UTF8.GetBytes(wins.ToString()))
                    w.Write(" "c)
                    w.Write(If(SpawnKey, "1"c, "0"c))
                    w.Write(" "c)
                    w.Write(Encoding.UTF8.GetBytes(leagueId.ToString()))
                    w.Write(" "c)
                    w.Write(Encoding.UTF8.GetBytes(highLadderRating.ToString()))
                    w.Write(" "c)
                    w.Write(Encoding.UTF8.GetBytes(ironManLadderRating.ToString()))
                    w.Write(" "c)
                    w.Write(Encoding.UTF8.GetBytes(ironManLadderRank.ToString()))
                    w.Write(" "c)
                    w.Write(iconCode)
                End If

                If Battlenet.Product.IsWarcraftIII(Product) Then
                    Dim iconCode = &H57335231
                    Dim ladderLevel = CUInt(0)
                    Dim clanTag = CType(ActiveAccount.[Get]("System\Clan", New Byte() {0, 0, 0, 0},), Byte())
                    w.Write(" "c)
                    w.Write(iconCode)
                    w.Write(" "c)
                    w.Write(Encoding.UTF8.GetBytes(ladderLevel.ToString()))

                    If Not clanTag.SequenceEqual(New Byte() {0, 0, 0, 0}) Then
                        w.Write(" "c)
                        w.Write(clanTag)
                    End If
                End If

            Finally

                If w Is Nothing Then
                    Statstring = buf
                Else
                    Statstring = New Byte(CInt(w.BaseStream.Position) - 1) {}
                    Buffer.BlockCopy(buf, 0, Statstring, 0, CInt(w.BaseStream.Position))
                    w.Close()
                End If

                If m IsNot Nothing Then m.Close()
            End Try
        End Sub

        Public Shared Function HasAdmin(ByVal user As GameState, ByVal Optional includeChannelOp As Boolean = False) As Boolean
            Dim grantSudoToSpoofedAdmins = Settings.GetBoolean(New String() {"battlenet", "emulation", "grant_sudo_to_spoofed_admins"}, False)
            Dim hasSudo = False

            SyncLock user
                Dim userFlags = CType(user.ActiveAccount.[Get](Account.FlagsKey), Account.Flags)
                hasSudo = (grantSudoToSpoofedAdmins AndAlso (user.ChannelFlags.HasFlag(Account.Flags.Admin) OrElse (user.ChannelFlags.HasFlag(Account.Flags.ChannelOp) AndAlso includeChannelOp) OrElse user.ChannelFlags.HasFlag(Account.Flags.Employee))) OrElse userFlags.HasFlag(Account.Flags.Admin) OrElse (userFlags.HasFlag(Account.Flags.ChannelOp) AndAlso includeChannelOp) OrElse userFlags.HasFlag(Account.Flags.Employee)
            End SyncLock

            Return hasSudo
        End Function

        Public Function HasAdmin(ByVal Optional includeChannelOp As Boolean = False) As Boolean
            Return HasAdmin(Me, includeChannelOp)
        End Function

#Disable Warning CA1822 ' Mark members as static
        Public Sub SetLocale()
#Enable Warning CA1822 ' Mark members as static
            Return
        End Sub

        Public Sub StopGameAd()
            If GameAd Is Nothing Then Return
            Dim removedGameAd = Nothing

            If Battlenet.Common.ActiveGameAds.TryRemove(GameAd.Name, removedGameAd) Then

                If removedGameAd <> GameAd Then
                    Battlenet.Common.ActiveGameAds.TryAdd(GameAd.Name, GameAd)
                End If
            End If
        End Sub
    End Class
End Namespace
