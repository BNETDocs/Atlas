Imports AtlasV.Localization
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class KickCommand
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

            If Not (varContext.GameState.ChannelFlags.HasFlag(Account.Flags.Admin) OrElse varContext.GameState.ChannelFlags.HasFlag(Account.Flags.ChannelOp) OrElse varContext.GameState.ChannelFlags.HasFlag(Account.Flags.Employee)) Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, CUInt(0), varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.YouAreNotAChannelOperator).WriteTo(varContext.GameState.Client)
                Return
            End If

            If Arguments.Count < 1 Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, CUInt(0), varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.UserNotLoggedOn).WriteTo(varContext.GameState.Client)
                Return
            End If

            Dim target = Arguments(0)
            Arguments.RemoveAt(0)
            Dim reason = String.Join(" ", Arguments)
            varContext.GameState.ActiveChannel.KickUser(varContext.GameState, target, reason)
        End Sub
    End Class
End Namespace
