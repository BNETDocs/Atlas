Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class ClanCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varRawBuffer As ChatCommandContext) As Boolean
            Return varRawBuffer IsNot Nothing AndAlso varRawBuffer.GameState IsNot Nothing AndAlso varRawBuffer.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varRawBuffer As ChatCommandContext)
            Dim hasAdmin = varRawBuffer.GameState.HasAdmin(True)
            Dim replyEventId = ChatEvent.EventIds.EID_ERROR
            Dim reply = String.Empty

            If Not hasAdmin OrElse varRawBuffer.GameState.ActiveChannel Is Nothing Then
                reply = Resources.YouAreNotAChannelOperator
            Else
                Dim subcommand = If(Arguments.Count > 0, Arguments(0), String.Empty)
                If Not String.IsNullOrEmpty(subcommand) Then Arguments.RemoveAt(0)

                Select Case subcommand.ToLower()
                    Case "motd"
                        varRawBuffer.GameState.ActiveChannel.SetTopic(String.Join(" ", Arguments))
                        Exit Select
                    Case "public", "pub"
                        varRawBuffer.GameState.ActiveChannel.SetAllowNewUsers(True)
                        Exit Select
                    Case "private", "priv"
                        varRawBuffer.GameState.ActiveChannel.SetAllowNewUsers(False)
                        Exit Select
                    Case Else
                        reply = Resources.InvalidChatCommand
                        Exit Select
                End Select
            End If

            If String.IsNullOrEmpty(reply) Then Return

            For Each kv In varRawBuffer.Environment
                reply = reply.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In reply.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(replyEventId, varRawBuffer.GameState.ChannelFlags, varRawBuffer.GameState.Ping, varRawBuffer.GameState.OnlineName, line).WriteTo(varRawBuffer.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
