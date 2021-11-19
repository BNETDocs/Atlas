Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class JoinCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            If Arguments.Count < 1 Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, CUInt(0), varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.InvalidChannelName).WriteTo(varContext.GameState.Client)
                Return
            End If

            Dim channelName = String.Join(" ", Arguments)
            Dim userFlags = Nothing
            varContext.GameState.ActiveAccount.[Get](Account.FlagsKey, userFlags)
            Dim ignoreLimits = (CType((CType(userFlags, AccountKeyValue)).Value, Account.Flags)).HasFlag(Account.Flags.Employee)
            Dim rereChannel = Channel.GetChannelByName(channelName, True)
            rereChannel.AcceptUser(varContext.GameState, ignoreLimits, False)
        End Sub
    End Class
End Namespace
