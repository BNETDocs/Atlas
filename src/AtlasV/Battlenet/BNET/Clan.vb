Imports AtlasV.Battlenet.Protocols.Game
Imports AtlasV.Battlenet.Protocols.Game.Messages
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Text

Namespace AtlasV.Battlenet
    Class Clan
        Implements IDisposable

        Public Enum Ranks As Byte
            Probation = 0
            Initiate = 1
            Member = 2
            Officer = 3
            Leader = 4
            NotInClan = 255
        End Enum

        Public Enum Results As Byte
            Success = 0
            NameInUse = 1
            TooSoon = 2
            NotEnough = 3
            Decline = 4
            Unavailable = 5
            Accept = 6
            NotAuthorized = 7
            NotAllowed = 8
            ClanIsFull = 9
            BadTag = 10
            BadName = 11
            UserNotFound = 12
        End Enum

        Public Property ActiveChannel As Channel
        Public Property Tag As Byte()
        Public Property Name As Byte()
        Public Property Users As ConcurrentDictionary(Of Byte(), Ranks)

        Public Sub New(ByVal varTag As Byte(), ByVal varName As Byte(), ByVal Optional varUsers As IDictionary(Of Byte(), Ranks) = Nothing)
            SetName(varName)
            SetTag(varTag)
            ActiveChannel = Channel.GetChannelByName($"Clan {Encoding.UTF8.GetString(varTag).Replace(vbNullChar, "")}", True)
            Users = New ConcurrentDictionary(Of Byte(), Ranks)(varUsers)
        End Sub

        Public Sub Close()
            If ActiveChannel IsNot Nothing Then
                If ActiveChannel.Count = 0 Then ActiveChannel.Dispose()
                ActiveChannel = Nothing
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Close()
        End Sub

        Public Function AddUser(ByVal username As Byte(), ByVal rank As Ranks) As Boolean
            Return Users.TryAdd(username, rank)
        End Function

        Public Function ContainsUser(ByVal username As Byte()) As Boolean
            Dim usernameStr = Encoding.UTF8.GetString(username)
            Dim varUsers = Users.ToArray()

            For Each n In varUsers
                Dim nStr As String = Encoding.UTF8.GetString(n.Key)

                If String.Equals(usernameStr, nStr, StringComparison.CurrentCultureIgnoreCase) Then
                    Return True
                End If
            Next

            Return False
        End Function

        Public Function GetUserRank(ByVal username As Byte(), ByRef rank As Ranks) As Boolean
            rank = Ranks.NotInClan
            Dim usernameStr = Encoding.UTF8.GetString(username)
            Dim varUsers = Users.ToArray()

            For Each n In varUsers
                Dim nStr As String = Encoding.UTF8.GetString(n.Key)

                If String.Equals(usernameStr, nStr, StringComparison.CurrentCultureIgnoreCase) Then
                    rank = n.Value
                    Return True
                End If
            Next

            Return False
        End Function

        Public Function RemoveUser(ByVal username As Byte()) As Boolean
            Dim dummyValue As Ranks
            Return Users.TryRemove(username, dummyValue)
        End Function

        Public Sub SetName(ByVal varName As Byte())
            If Name.Length < 1 Then Throw New ArgumentOutOfRangeException($"Clan name must be at least 1 byte in length")
            Name = varName
        End Sub

        Public Sub SetTag(ByVal tag As Byte())
            If tag.Length <> 4 Then Throw New ArgumentOutOfRangeException($"Clan tag must be exactly 4 bytes in length")
            tag = tag
            WriteClanInfo()
            If ActiveChannel IsNot Nothing Then ActiveChannel.SetName($"Clan {Encoding.UTF8.GetString(tag).Replace(vbNullChar, "")}")
        End Sub

        Protected Sub WriteClanInfo()
            If Users Is Nothing Then Return
            Dim message = New SID_CLANINFO()
            Dim varUsers = Users.ToArray()
            Dim gameState = Nothing

            For Each n In varUsers
                Dim nStr As String = Encoding.UTF8.GetString(n.Key)

                If Not Common.GetClientByOnlineName(nStr, gameState) Then
                    Continue For
                End If

                Dim arguments = New Dictionary(Of String, Object)() From {
                    {"tag", Tag},
                    {"rank", n.Value}
                }
                message.Invoke(New MessageContext(gameState.Client, Protocols.MessageDirection.ServerToClient, arguments))
                gameState.Client.Send(message.ToByteArray(gameState.Client.ProtocolType))
            Next
        End Sub

        Public Sub WriteStatusChange(ByVal target As GameState, ByVal online As Boolean)
            Dim targetUsernameBS = Encoding.UTF8.GetBytes(target.Username)
            Dim rank = Nothing

            If Not GetUserRank(targetUsernameBS, rank) Then
                Logging.WriteLine(Logging.LogLevel.[Error], Logging.LogType.Clan, "Unable to find target in clan object, cannot write status change without rank")
                Return
            End If

            WriteMessageToUsers(New SID_CLANMEMBERSTATUSCHANGE(), New Dictionary(Of String, Object)() From {
                {"username", target.Username},
                {"rank", rank},
                {"status", CByte((If(online, 1, 0)))},
                {"location", Array.Empty(Of Byte)()}
            })
        End Sub

        Public Sub WriteMessageToUsers(ByVal varMessage As Message, ByVal varArguments As Dictionary(Of String, Object))
            If Users Is Nothing Then Return
            Dim varUsers = Users.ToArray()
            Dim gameState = Nothing

            For Each n In varUsers
                Dim nStr As String = Encoding.UTF8.GetString(n.Key)

                If Not Common.GetClientByOnlineName(nStr, gameState) Then
                    Continue For
                End If

                varMessage.Invoke(New MessageContext(gameState.Client, Protocols.MessageDirection.ServerToClient, varArguments))
                gameState.Client.Send(varMessage.ToByteArray(gameState.Client.ProtocolType))
            Next
        End Sub

    End Class
End Namespace
