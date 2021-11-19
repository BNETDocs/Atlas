Imports AtlasV.Localization
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class EmoteCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            If varContext.GameState.ActiveChannel Is Nothing Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, CUInt(0), varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.InvalidChatCommand).WriteTo(varContext.GameState.Client)
                Return
            End If

            varContext.GameState.ActiveChannel.WriteChatMessage(varContext.GameState, RawBuffer, True)

            If varContext.GameState.ActiveChannel.Count <= 1 OrElse varContext.GameState.ActiveChannel.ActiveFlags.HasFlag(Channel.Flags.Silent) Then
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ActiveChannel.ActiveFlags, 0, varContext.GameState.ActiveChannel.Name, Resources.NoOneHearsYou).WriteTo(varContext.GameState.Client)
            End If
        End Sub
    End Class
End Namespace
