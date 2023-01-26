Imports AtlasV.Battlenet.Protocols.Game.Messages
Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class FriendCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim replyEventId = ChatEvent.EventIds.EID_ERROR
            Dim reply = String.Empty
            Dim subcommand = If(Arguments.Count > 0, Arguments(0), String.Empty)

            If Not String.IsNullOrEmpty(subcommand) Then
                Arguments.RemoveAt(0)
                Dim stripSize = subcommand.Length + (If(RawBuffer.Length - subcommand.Length > 0, 1, 0))
                RawBuffer = RawBuffer.Skip(stripSize).ToArray()
            End If

            Dim friends = CType(varContext.GameState.ActiveAccount.[Get](Account.FriendsKey, New List(Of Byte())(),), List(Of Byte()))

            Select Case subcommand.ToLower()
                Case "add", "a"
                    Dim targetString = If(Arguments.Count > 0, Arguments(0), String.Empty)

                    If String.IsNullOrEmpty(targetString) Then
                        reply = Resources.AddFriendEmptyTarget
                    Else
                        Dim exists = False

                        For Each friendByteString In friends
                            Dim friendString As String = Encoding.UTF8.GetString(friendByteString)

                            If String.Equals(targetString, friendString, StringComparison.CurrentCultureIgnoreCase) Then
                                exists = True
                                Exit For
                            End If
                        Next

                        If exists Then
                            reply = Resources.AlreadyAddedFriend.Replace("{friend}", targetString)
                        Else
                            Dim friendByteString = Encoding.UTF8.GetBytes(targetString)
                            Dim [friend] = New [Friend](varContext.GameState, friendByteString)
                            friends.Add([friend].Username)
                            replyEventId = ChatEvent.EventIds.EID_INFO
                            reply = Resources.AddedFriend.Replace("{friend}", Encoding.UTF8.GetString([friend].Username))
                            Call New SID_FRIENDSADD().Invoke(New MessageContext(varContext.GameState.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                                {"friend", [friend]}
                            }))
                        End If
                    End If

                    Exit Select
                Case "demote", "d"
                    Dim targetString = If(Arguments.Count > 0, Arguments(0), String.Empty)

                    If String.IsNullOrEmpty(targetString) Then
                        reply = Resources.DemoteFriendEmptyTarget
                    Else
                        Dim exists As Byte() = Nothing
                        Dim counter1 As Byte = 0
                        Dim counter2 As Byte = 0

                        For Each friendByteString In friends
                            Dim friendString As String = Encoding.UTF8.GetString(friendByteString)

                            If String.Equals(targetString, friendString, StringComparison.CurrentCultureIgnoreCase) Then
                                exists = friendByteString
                                Exit For
                            End If

                            counter1 += 1
                        Next

                        If exists Is Nothing OrElse exists.Length = 0 Then
                            reply = Resources.DemoteFriendEmptyTarget
                        Else

                            If counter1 = friends.Count - 1 Then
                                counter2 = counter1
                            Else
                                counter2 = CByte((counter1 + 1))
                                friends.RemoveAt(counter1)
                                friends.Insert(counter2, exists)
                            End If

                            replyEventId = ChatEvent.EventIds.EID_INFO
                            reply = Resources.DemotedFriend.Replace("{friend}", Encoding.UTF8.GetString(exists))
                            If counter1 <> counter2 Then Call New SID_FRIENDSPOSITION().Invoke(New MessageContext(varContext.GameState.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                                {"old", counter1},
                                {"new", counter2}
                            }))
                        End If
                    End If

                    Exit Select
                Case "list", "l"
                    replyEventId = ChatEvent.EventIds.EID_INFO
                    reply = Resources.YourFriendsList
                    Dim friendCount = 0

                    For Each [friend] In friends
                        If Math.Min(System.Threading.Interlocked.Increment(friendCount), friendCount - 1) = 0 Then reply += Battlenet.Common.NewLine
                        Dim friendString = Encoding.UTF8.GetString([friend])
                        reply += $"{friendCount}: {friendString}{Battlenet.Common.NewLine}"
                    Next

                    If friendCount > 0 Then
                        'reply = reply(0..(reply.Length - Battlenet.Common.NewLine.Length))
                        reply = reply.Substring(0, reply.Length - Battlenet.Common.NewLine.Length)
                    End If
                    Exit Select
                Case "promote", "p"
                    Dim targetString = If(Arguments.Count > 0, Arguments(0), String.Empty)

                    If String.IsNullOrEmpty(targetString) Then
                        reply = Resources.PromoteFriendEmptyTarget
                    Else
                        Dim exists As Byte() = Nothing
                        Dim counter1 As Byte = 0
                        Dim counter2 As Byte = 0

                        For Each friendByteString In friends
                            Dim friendString As String = Encoding.UTF8.GetString(friendByteString)

                            If String.Equals(targetString, friendString, StringComparison.CurrentCultureIgnoreCase) Then
                                exists = friendByteString
                                Exit For
                            End If

                            counter1 += 1
                        Next

                        If exists Is Nothing OrElse exists.Length = 0 Then
                            reply = Resources.PromoteFriendEmptyTarget
                        Else

                            If counter1 = 0 Then
                                counter2 = counter1
                            Else
                                counter2 = CByte((counter1 - 1))
                                friends.RemoveAt(counter1)
                                friends.Insert(counter2, exists)
                            End If

                            replyEventId = ChatEvent.EventIds.EID_INFO
                            reply = Resources.PromotedFriend.Replace("{friend}", Encoding.UTF8.GetString(exists))
                            If counter1 <> counter2 Then Call New SID_FRIENDSPOSITION().Invoke(New MessageContext(varContext.GameState.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                                {"old", counter1},
                                {"new", counter2}
                            }))
                        End If
                    End If

                    Exit Select
                Case "remove", "rem", "r"
                    Dim targetString = If(Arguments.Count > 0, Arguments(0), String.Empty)

                    If String.IsNullOrEmpty(targetString) Then
                        reply = Resources.RemoveFriendEmptyTarget
                    Else
                        Dim exists As Byte() = Nothing
                        Dim counter As Byte = 0

                        For Each friendByteString In friends
                            Dim friendString As String = Encoding.UTF8.GetString(friendByteString)

                            If String.Equals(targetString, friendString, StringComparison.CurrentCultureIgnoreCase) Then
                                exists = friendByteString
                                Exit For
                            End If

                            counter += 1
                        Next

                        If exists Is Nothing OrElse exists.Length = 0 Then
                            reply = Resources.AlreadyRemovedFriend.Replace("{friend}", targetString)
                        Else
                            replyEventId = ChatEvent.EventIds.EID_INFO
                            reply = Resources.RemovedFriend.Replace("{friend}", targetString)
                            friends.Remove(exists)
                            Call New SID_FRIENDSREMOVE().Invoke(New MessageContext(varContext.GameState.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                                {"friend", counter}
                            }))
                        End If
                    End If

                    Exit Select
                Case Else
                    reply = Resources.InvalidChatCommand
                    Exit Select
            End Select

            If String.IsNullOrEmpty(reply) Then Return

            For Each kv In varContext.Environment
                reply = reply.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In reply.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(replyEventId, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
