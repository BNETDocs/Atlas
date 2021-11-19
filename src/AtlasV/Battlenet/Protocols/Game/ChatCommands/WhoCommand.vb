Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class WhoCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim channelName = String.Join(" ", Arguments)
            Dim ch = If(channelName.Length > 0, Channel.GetChannelByName(channelName, False), varContext.GameState.ActiveChannel)
            Dim r As String

            If ch Is Nothing Then
                r = Resources.ChannelNotFound

                For Each line In r.Split(Battlenet.Common.NewLine)
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
                Next

                Return
            End If

            If ch.ActiveFlags.HasFlag(Channel.Flags.Restricted) Then
                r = Resources.ChannelIsRestricted

                For Each line In r.Split(Battlenet.Common.NewLine)
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, ch.ActiveFlags, 0, ch.Name, line).WriteTo(varContext.GameState.Client)
                Next

                Return
            End If

            r = Resources.WhoCommand
            r = r.Replace("{channel}", If(ch Is Nothing, "(null)", ch.Name))
            r = r.Replace("{users}", ch.GetUsersAsString(varContext.GameState))

            For Each kv In varContext.Environment
                r = r.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In r.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
