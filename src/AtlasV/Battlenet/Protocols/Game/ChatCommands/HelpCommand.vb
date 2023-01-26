Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class HelpCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim hasAdmin = varContext.GameState.HasAdmin()
            Dim topic = If(Arguments.Count > 0, Arguments(0), String.Empty)
            If Not String.IsNullOrEmpty(topic) Then Arguments.RemoveAt(0)
            Dim remarks = If(hasAdmin, Resources.HelpCommandRemarksWithAdmin, Resources.HelpCommandRemarks)

            Select Case topic.ToLower()
                Case "admin"

                    If hasAdmin Then
                        Call New AdminHelpCommand(RawBuffer, Arguments).Invoke(varContext)
                        Return
                    End If

                    Exit Select
                Case "advanced"
                    remarks = Resources.HelpCommandAdvancedRemarks
                Case "aliases"
                    remarks = Resources.HelpCommandAliasesRemarks
                Case "ban"
                    remarks = Resources.HelpCommandBanRemarks
                Case "channel", "join", "j"
                    remarks = Resources.HelpCommandJoinRemarks
                Case "commands"
                    remarks = Resources.HelpCommandCommandsRemarks
                Case "time"
                    remarks = Resources.HelpCommandTimeRemarks
            End Select

            For Each kv In varContext.Environment
                remarks = remarks.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In remarks.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
