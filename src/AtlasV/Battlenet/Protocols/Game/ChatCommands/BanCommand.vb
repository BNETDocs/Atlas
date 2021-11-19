Imports AtlasV.Localization
Imports System.Collections.Generic
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class BanCommand
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
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.YouAreNotAChannelOperator).WriteTo(varContext.GameState.Client)
                Return
            End If

            Dim target = ""

            If Arguments.Count > 0 Then
                target = Arguments(0)
                Arguments.RemoveAt(0)
                RawBuffer = RawBuffer.Skip(Encoding.UTF8.GetByteCount(target) + (If(Arguments.Count > 0, 1, 0))).ToArray()
            End If

            Dim targetState = Nothing

            If String.IsNullOrEmpty(target) OrElse Not Battlenet.Common.ActiveGameStates.TryGetValue(target, targetState) OrElse targetState Is Nothing Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.UserNotLoggedOn).WriteTo(varContext.GameState.Client)
                Return
            End If

            varContext.GameState.ActiveChannel.BanUser(varContext.GameState, targetState, String.Join(" ", Arguments))
        End Sub
    End Class
End Namespace
