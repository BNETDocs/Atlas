Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class AdminChannelCommand
        Inherits ChatCommand

        Public Sub New(ByVal rawBuffer As Byte(), ByVal arguments As List(Of String))
            MyBase.New(rawBuffer, arguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal context As ChatCommandContext) As Boolean
            Return context IsNot Nothing AndAlso context.GameState IsNot Nothing AndAlso context.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal context As ChatCommandContext)
            Dim subcommand = If(Arguments.Count > 0, Arguments(0), String.Empty)

            If Not String.IsNullOrEmpty(subcommand) Then
                Arguments.RemoveAt(0)
            End If

            Dim eventId = ChatEvent.EventIds.EID_ERROR
            Dim reply As String = Resources.InvalidAdminCommand
            Dim channel = context.GameState.ActiveChannel
            Dim flags = Nothing, maxUsers = Nothing

            If channel IsNot Nothing Then

                Select Case subcommand.ToLower()
                    Case "flags", "flag"

                        If Arguments.Count < 1 Then
                            eventId = ChatEvent.EventIds.EID_ERROR
                            reply = Resources.InvalidAdminCommand
                        Else
                            Integer.TryParse(Arguments(0), flags)
                            reply = String.Empty
                            channel.SetActiveFlags(CType(flags, Channel.Flags))
                        End If

                        Exit Select
                    Case "rename", "name"
                        Dim newName = String.Join(" ", Arguments)

                        If Not String.IsNullOrEmpty(newName) Then
                            reply = String.Empty

                            SyncLock Battlenet.Common.ActiveChannels
                                Battlenet.Common.ActiveChannels.Remove(channel.Name)
                                Battlenet.Common.ActiveChannels.Add(newName, channel)
                            End SyncLock

                            channel.SetName(newName)
                        End If

                        Exit Select
                    Case "maxusers", "maxuser"

                        If Arguments.Count < 1 Then
                            eventId = ChatEvent.EventIds.EID_ERROR
                            reply = Resources.InvalidAdminCommand
                        Else
                            Integer.TryParse(Arguments(0), maxUsers)
                            reply = String.Empty
                            channel.SetMaxUsers(maxUsers)
                        End If

                        Exit Select
                    Case "resync", "sync"
                        reply = String.Empty
                        channel.Resync()
                        Exit Select
                    Case "topic"
                        reply = String.Empty
                        channel.SetTopic(String.Join(" ", Arguments))
                        Exit Select
                End Select
            End If

            If String.IsNullOrEmpty(reply) Then Return

            For Each kv In context.Environment
                reply = reply.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In reply.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(eventId, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
