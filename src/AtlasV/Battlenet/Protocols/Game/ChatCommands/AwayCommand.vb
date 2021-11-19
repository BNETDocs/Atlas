Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class AwayCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim message As String = If(Arguments.Count = 0, Nothing, String.Join(" ", Arguments))
            Dim r As String

            If varContext.GameState.Away Is Nothing OrElse Not String.IsNullOrEmpty(message) Then
                varContext.GameState.Away = If(String.IsNullOrEmpty(message), "Not available", message)
                r = Resources.AwayCommandOn
            Else
                varContext.GameState.Away = Nothing
                r = Resources.AwayCommandOff
            End If

            For Each kv In varContext.Environment
                r = r.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In r.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
