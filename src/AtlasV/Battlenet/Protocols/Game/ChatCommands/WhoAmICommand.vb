Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class WhoAmICommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim ch = varContext.GameState.ActiveChannel
            Dim r = If(ch Is Nothing, Resources.YouAreUsingGameInRealm, Resources.YouAreUsingGameInTheChannel)

            If varContext.GameState.Away IsNot Nothing Then
                r += Battlenet.Common.NewLine + Resources.AwayCommandStatusSelf.Replace("{awayMessage}", varContext.GameState.Away)
            End If

            r = r.Replace("{channel}", If(ch Is Nothing, "(null)", ch.Name))
            r = r.Replace("{realm}", "BNETDocs")

            For Each kv In varContext.Environment
                r = r.Replace("{" & kv.Key & "}", kv.Value)
            Next

            Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, r).WriteTo(varContext.GameState.Client)
        End Sub
    End Class
End Namespace
