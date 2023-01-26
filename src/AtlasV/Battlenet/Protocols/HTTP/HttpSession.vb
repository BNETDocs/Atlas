Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.Net
Imports System.Net.Sockets
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Http
    Public Class HttpSession
        Public Property Client As Socket = Nothing

        Public Sub New(ByVal inSocket As Socket)
            If inSocket Is Nothing Then Return
            If Not inSocket.Connected Then
                Return
            End If

            Client = inSocket
        End Sub

        Public Sub ConnectedEvent()
            If Client Is Nothing Then Return

            Dim assembly = GetType(Program).Assembly
            Dim server = $"{assembly.GetName().Name}/{assembly.GetName().Version} ({Program.DistributionMode})"
            Dim activeUsers = $"{Battlenet.Common.ActiveGameStates.Count}"
            Dim systemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
            Dim systemUptimeStr = $"{Math.Floor(systemUptime.TotalDays)} day{(If(Math.Floor(systemUptime.TotalDays) = 1, "", "s"))} {(If(systemUptime.Hours < 10, "0", ""))}{systemUptime.Hours}:{(If(systemUptime.Minutes < 10, "0", ""))}{systemUptime.Minutes}:{(If(systemUptime.Seconds < 10, "0", ""))}{systemUptime.Seconds}"
            Dim processUptime = TimeSpan.FromMilliseconds(Environment.TickCount64 - Program.TickCountAtInit)
            Dim processUptimeStr = $"{Math.Floor(processUptime.TotalDays)} day{(If(Math.Floor(processUptime.TotalDays) = 1, "", "s"))} {(If(processUptime.Hours < 10, "0", ""))}{processUptime.Hours}:{(If(processUptime.Minutes < 10, "0", ""))}{processUptime.Minutes}:{(If(processUptime.Seconds < 10, "0", ""))}{processUptime.Seconds}"
            Dim replyBody = String.Empty
            Dim replyCode As String
            Dim replyHeaders = New List(Of HttpHeader)() From {
                New HttpHeader("Cache-Control", "must-revalidate,no-cache,no-store,max-age=0"),
                New HttpHeader("Connection", "close"),
                New HttpHeader("Date", $"{DateTime.Now.ToUniversalTime()} GMT"),
                New HttpHeader("Server", server)
            }
            Dim locFile = New BNFTP.File("www/index.shtml")

            If locFile Is Nothing OrElse Not locFile.Exists Then
                replyCode = "404 Not Found"
            ElseIf Not locFile.OpenStream() Then
                replyCode = "403 Forbidden"
            Else
                replyBody = locFile.StreamReader.ReadToEnd()
                replyBody = replyBody.Replace("<!--#activeUsers#-->", activeUsers, StringComparison.OrdinalIgnoreCase)
                replyBody = replyBody.Replace("<!--#processUptime#-->", processUptimeStr, StringComparison.OrdinalIgnoreCase)
                replyBody = replyBody.Replace("<!--#systemUptime#-->", systemUptimeStr, StringComparison.OrdinalIgnoreCase)
                replyCode = "200 OK"
                replyHeaders.Add(New HttpHeader("Content-Length", $"{replyBody.Length}"))
                replyHeaders.Add(New HttpHeader("Content-Type", "text/html;charset=utf-8"))
            End If

            Dim r As String = $"HTTP/1.0 {replyCode}\r\n"

            For Each h In replyHeaders
                r += h.ToString()
            Next

            r += vbCrLf
            r += replyBody
            Client.Send(Encoding.UTF8.GetBytes(r))
            Client.Disconnect(True)
        End Sub
    End Class
End Namespace