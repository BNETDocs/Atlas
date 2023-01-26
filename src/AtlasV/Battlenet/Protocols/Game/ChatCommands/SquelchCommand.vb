Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.Net

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class SquelchCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim r As String
            Dim t As String
            t = If(Arguments.Count = 0, "", Arguments(0))
            Dim target As GameState = Nothing

            If Not Battlenet.Common.GetClientByOnlineName(t, target) OrElse target Is Nothing Then
                r = Resources.UserNotLoggedOn

                For Each line In r.Split(Environment.NewLine)
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
                Next

                Return
            End If

            Dim rereIpAddress = IPAddress.Parse(target.Client.RemoteEndPoint.ToString().Split(":"c)(0))

            SyncLock varContext.GameState.SquelchedIPs

                If Not varContext.GameState.SquelchedIPs.Contains(rereIpAddress) Then
                    varContext.GameState.SquelchedIPs.Add(rereIpAddress)
                End If
            End SyncLock

            SyncLock varContext.GameState.ActiveChannel
                If varContext.GameState.ActiveChannel IsNot Nothing Then varContext.GameState.ActiveChannel.SquelchUpdate(varContext.GameState)
            End SyncLock
        End Sub
    End Class
End Namespace
