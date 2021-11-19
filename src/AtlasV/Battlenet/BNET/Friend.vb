Imports AtlasV.Battlenet.Protocols.Game
Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace AtlasV.Battlenet
    Class [Friend]
        Public Enum Location As Byte
            Offline = &H0
            NotInChat = &H1
            InChat = &H2
            InPublicGame = &H3
            InPrivateGame = &H4
            InPasswordGame = &H5
        End Enum

        <Flags>
        Public Enum Status As Byte
            None = &H0
            Mutual = &H1
            DoNotDisturb = &H2
            Away = &H4
        End Enum

        Public Property LocationId As Location
        Public Property LocationString As Byte()
        Public Property StatusId As Status
        Public Property ProductCode As Product.ProductCode
        Public Property Username As Byte()

        Public Sub New(ByVal varSource As GameState, ByVal varUsername As Byte())
            Username = varUsername
            Sync(varSource)
        End Sub

        Public Sub Sync(ByVal source As GameState)
            LocationId = Location.Offline
            LocationString = Array.Empty(Of Byte)()
            ProductCode = Product.ProductCode.None
            StatusId = Status.None
            Dim target As GameState = Nothing

            If source Is Nothing OrElse source.ActiveAccount Is Nothing OrElse Not Common.GetClientByOnlineName(Encoding.UTF8.GetString(Username), target) OrElse target Is Nothing OrElse target.ActiveAccount Is Nothing Then
                Return
            End If

            SyncLock source

                SyncLock target
                    Dim admin = source.HasAdmin()
                    Dim mutual = False
                    Dim sourceFriendStrings = CType(source.ActiveAccount.[Get](Account.FriendsKey, New List(Of Byte())(),), List(Of Byte()))
                    Dim targetFriendStrings = CType(target.ActiveAccount.[Get](Account.FriendsKey, New List(Of Byte())(),), List(Of Byte()))

                    For Each targetFriendString In targetFriendStrings

                        For Each sourceFriendString In sourceFriendStrings
                            Dim aString As String = Encoding.UTF8.GetString(sourceFriendString)
                            Dim bString As String = Encoding.UTF8.GetString(targetFriendString)

                            If String.Equals(aString, bString, StringComparison.CurrentCultureIgnoreCase) Then
                                mutual = True
                                Exit For
                            End If
                        Next

                        If mutual Then Exit For
                    Next

                    If mutual Then StatusId = StatusId Or Status.Mutual
                    If Not String.IsNullOrEmpty(target.Away) Then StatusId = StatusId Or Status.Away
                    If Not String.IsNullOrEmpty(target.DoNotDisturb) Then StatusId = StatusId Or Status.DoNotDisturb

                    If target.ActiveChannel Is Nothing AndAlso target.GameAd Is Nothing Then
                        LocationId = Location.NotInChat
                    ElseIf target.ActiveChannel IsNot Nothing Then
                        LocationId = Location.InChat
                        If mutual OrElse admin Then LocationString = Encoding.UTF8.GetBytes(target.ActiveChannel.Name)
                    ElseIf target.GameAd IsNot Nothing Then

                        If Not target.GameAd.ActiveStateFlags.HasFlag(GameAd.StateFlags.[Private]) AndAlso target.GameAd.Password.Length = 0 Then
                            LocationId = Location.InPublicGame
                            LocationString = target.GameAd.Name
                        ElseIf Not (mutual OrElse admin) Then
                            LocationId = Location.InPrivateGame
                        Else
                            LocationId = Location.InPasswordGame
                            LocationString = target.GameAd.Name
                        End If
                    End If
                End SyncLock
            End SyncLock
        End Sub
    End Class
End Namespace
