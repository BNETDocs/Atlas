Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class ReJoinCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            If varContext.GameState.ActiveChannel Is Nothing Then
                Call New InvalidCommand(RawBuffer, Arguments).Invoke(varContext)
                Return
            End If

            If Not (varContext.GameState.ChannelFlags.HasFlag(Account.Flags.Employee) OrElse varContext.GameState.ChannelFlags.HasFlag(Account.Flags.ChannelOp) OrElse varContext.GameState.ChannelFlags.HasFlag(Account.Flags.Admin)) Then
                Return
            End If

            Channel.MoveUser(varContext.GameState, varContext.GameState.ActiveChannel, True)
        End Sub
    End Class
End Namespace
