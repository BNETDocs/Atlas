Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class AdminBroadcastCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Task.Run(Sub()
                         Dim lamChatEvent = New ChatEvent(ChatEvent.EventIds.EID_BROADCAST, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, RawBuffer)

                         SyncLock Battlenet.Common.ActiveGameStates
                             For Each pair In Battlenet.Common.ActiveGameStates
                                 lamChatEvent.WriteTo(pair.Value.Client)
                             Next
                         End SyncLock
                     End Sub)
        End Sub
    End Class
End Namespace
