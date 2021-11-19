using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Http
{
    class HttpSession
    {
        public Socket Client { get; private set; } = null;

        public HttpSession(Socket client)
        {
            if (client == null) { return; } //Yes this is possible.
            if (!client.Connected)
            {
                return;
            }

            Client = client;
        }

        public void ConnectedEvent()
        {
            if (Client == null) { return; } //Yes this is possible.
            var assembly = typeof(Program).Assembly;
            var server = $"{assembly.GetName().Name}/{assembly.GetName().Version} ({Program.DistributionMode})";

            var activeUsers = $"{Battlenet.Common.ActiveGameStates.Count:d}";

            var systemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            var systemUptimeStr = $"{Math.Floor(systemUptime.TotalDays)} day{(Math.Floor(systemUptime.TotalDays) == 1 ? "" : "s")} {(systemUptime.Hours < 10 ? "0" : "")}{systemUptime.Hours}:{(systemUptime.Minutes < 10 ? "0" : "")}{systemUptime.Minutes}:{(systemUptime.Seconds < 10 ? "0" : "")}{systemUptime.Seconds}";

            var processUptime = TimeSpan.FromMilliseconds(Environment.TickCount64 - Program.TickCountAtInit);
            var processUptimeStr = $"{Math.Floor(processUptime.TotalDays)} day{(Math.Floor(processUptime.TotalDays) == 1 ? "" : "s")} {(processUptime.Hours < 10 ? "0" : "")}{processUptime.Hours}:{(processUptime.Minutes < 10 ? "0" : "")}{processUptime.Minutes}:{(processUptime.Seconds < 10 ? "0" : "")}{processUptime.Seconds}";

            var replyBody = string.Empty;
            string replyCode;
            var replyHeaders = new List<HttpHeader>()
            {
                new HttpHeader("Cache-Control", "must-revalidate,no-cache,no-store,max-age=0"),
                new HttpHeader("Connection", "close"),
                new HttpHeader("Date", $"{DateTime.Now.ToUniversalTime():ddd, dd MMM yyyy HH:mm:ss} GMT"),
                new HttpHeader("Server", server),
            };

            var file = new BNFTP.File("www/index.shtml");
            if (file == null || !file.Exists)
            {
                replyCode = "404 Not Found";
            }
            else if (!file.OpenStream())
            {
                replyCode = "403 Forbidden";
            }
            else
            {
                replyBody = file.StreamReader.ReadToEnd();

                replyBody = replyBody.Replace("<!--#activeUsers#-->", activeUsers, StringComparison.OrdinalIgnoreCase);
                replyBody = replyBody.Replace("<!--#processUptime#-->", processUptimeStr, StringComparison.OrdinalIgnoreCase);
                replyBody = replyBody.Replace("<!--#systemUptime#-->", systemUptimeStr, StringComparison.OrdinalIgnoreCase);

                replyCode = "200 OK";
                replyHeaders.Add(new HttpHeader("Content-Length", $"{replyBody.Length:d}"));
                replyHeaders.Add(new HttpHeader("Content-Type", "text/html;charset=utf-8"));
            }

            string r = $"HTTP/1.0 {replyCode}\r\n";
            foreach (var h in replyHeaders) r += h.ToString();
            r += "\r\n";
            r += replyBody;

            Client.Send(Encoding.UTF8.GetBytes(r));
            Client.Disconnect(true);
        }
    }
}
