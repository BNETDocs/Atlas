Imports AtlasV.Localization
Imports System.Collections.Generic
Imports System

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class VersionCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim assembly = GetType(Program).Assembly
            Dim server = $"{assembly.GetName().Name}/{assembly.GetName().Version} ({Program.DistributionMode})"
            Dim systemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
            Dim systemUptimeStr = $"{Math.Floor(systemUptime.TotalDays)} day{(If(Math.Floor(systemUptime.TotalDays) = 1, "", "s"))} {(If(systemUptime.Hours < 10, "0", ""))}{systemUptime.Hours}:{(If(systemUptime.Minutes < 10, "0", ""))}{systemUptime.Minutes}:{(If(systemUptime.Seconds < 10, "0", ""))}{systemUptime.Seconds}"
            Dim processUptime = TimeSpan.FromMilliseconds(Environment.TickCount64 - Program.TickCountAtInit)
            Dim processUptimeStr = $"{Math.Floor(processUptime.TotalDays)} day{(If(Math.Floor(processUptime.TotalDays) = 1, "", "s"))} {(If(processUptime.Hours < 10, "0", ""))}{processUptime.Hours}:{(If(processUptime.Minutes < 10, "0", ""))}{processUptime.Minutes}:{(If(processUptime.Seconds < 10, "0", ""))}{processUptime.Seconds}"
            Dim hasAdmin = varContext.GameState.HasAdmin()
            Dim r As String = If(hasAdmin, Resources.VersionCommandWithAdmin, Resources.VersionCommand)
            varContext.Environment("version") = server
            varContext.Environment("systemUptime") = systemUptimeStr
            varContext.Environment("processUptime") = processUptimeStr

            For Each kv In varContext.Environment
                r = r.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In r.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
