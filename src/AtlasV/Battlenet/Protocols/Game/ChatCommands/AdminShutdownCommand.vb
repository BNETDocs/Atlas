Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class AdminShutdownCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim periodStr = If(Arguments.Count = 0, "", Arguments(0))
            If Arguments.Count > 0 Then Arguments.RemoveAt(0)
            Dim message = String.Join(" "c, Arguments)
            If message.Length = 0 Then message = Nothing

            If periodStr.Length = 0 Then
                periodStr = "30"
            End If

            Dim periodDbl = Nothing

            If periodStr.Equals("cancel") Then
                Battlenet.Common.ScheduleShutdownCancelled(message, varContext)
            Else

                If Not Double.TryParse(periodStr, periodDbl) Then
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, CUInt(0), varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.AdminShutdownCommandParseError).WriteTo(varContext.GameState.Client)
                    Return
                End If

                Battlenet.Common.ScheduleShutdown(TimeSpan.FromSeconds(periodDbl), message, varContext)
            End If
        End Sub
    End Class
End Namespace
