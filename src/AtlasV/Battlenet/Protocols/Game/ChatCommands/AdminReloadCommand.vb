Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class AdminReloadCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim e As Exception
            Dim r As String
            Dim eid As ChatEvent.EventIds

            Try
                Settings.Load()
                e = Nothing
                r = Resources.AdminReloadCommandSuccess
                eid = ChatEvent.EventIds.EID_INFO
            Catch ex As Exception
                e = ex
                r = Resources.AdminReloadCommandFailure.Replace("{exception}", e.[GetType]().Name)
                eid = ChatEvent.EventIds.EID_ERROR
            End Try

            For Each kv In varContext.Environment
                r = r.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In r.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(eid, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
            Next

            If e IsNot Nothing Then
                Throw e
            End If
        End Sub
    End Class
End Namespace
