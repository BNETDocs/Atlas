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
            if (!client.Connected)
            {
                return;
            }

            Client = client;
        }

        public void ConnectedEvent()
        {
            var assembly = typeof(Program).Assembly;
            var server = $"{assembly.GetName().Name}/{assembly.GetName().Version} ({Program.DistributionMode})";

            var systemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            var systemUptimeStr = $"{Math.Floor(systemUptime.TotalDays)} day{(Math.Floor(systemUptime.TotalDays) == 1 ? "" : "s")} {(systemUptime.Hours < 10 ? "0" : "")}{systemUptime.Hours}:{(systemUptime.Minutes < 10 ? "0" : "")}{systemUptime.Minutes}:{(systemUptime.Seconds < 10 ? "0" : "")}{systemUptime.Seconds}";

            var processUptime = TimeSpan.FromMilliseconds(Environment.TickCount64 - Program.TickCountAtInit);
            var processUptimeStr = $"{Math.Floor(processUptime.TotalDays)} day{(Math.Floor(processUptime.TotalDays) == 1 ? "" : "s")} {(processUptime.Hours < 10 ? "0" : "")}{processUptime.Hours}:{(processUptime.Minutes < 10 ? "0" : "")}{processUptime.Minutes}:{(processUptime.Seconds < 10 ? "0" : "")}{processUptime.Seconds}";

            string r = "";

            r += "HTTP/1.0 503 Service Unavailable\r\n";
            r += "Connection: close\r\n";
            r += "Content-Type: text/html;charset=utf-8\r\n";
            r += $"Date: {DateTime.Now.ToUniversalTime():ddd, dd MMM yyyy HH:mm:ss} GMT\r\n";
            r += $"Server: {server}\r\n";
            r += "\r\n";

            r += "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n<title>Atlas</title>\r\n";
            r += "<link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bootstrap@4.5.3/dist/css/bootstrap.min.css\" integrity=\"sha384-TX8t27EcRE3e/ihU7zmQxVncDAy5uIKz4rEkgIXeMed4M0jlfIDPvg6uqKI2xXr2\" crossorigin=\"anonymous\"/>\r\n";
            r += "</head><body style=\"background:#000;color:#fff;\">\r\n";

            r += "<div class=\"container\">\r\n";
            r += "<div class=\"mt-2 alert alert-danger\"><strong>Danger:</strong> This HTTP endpoint is extremely fragile. Please be gentle.</div>\r\n";
            r += "<h1>Atlas</h1>\r\n";
            r += "<h2>Summary</h2>\r\n";
            r += "<table class=\"table table-dark table-hover table-striped\">\r\n";
            r += "<thead></thead><tbody>\r\n";
            r += $"<tr><th>Process Uptime:</th><td>{processUptimeStr}</td></tr>";
            r += $"<tr><th>System Uptime:</th><td>{systemUptimeStr}</td></tr>";
            r += "</tbody></table>\r\n";
            r += "</div>\r\n";

            r += "<script src=\"https://code.jquery.com/jquery-3.5.1.slim.min.js\" integrity=\"sha384-DfXdz2htPH0lsSSs5nCTpuj/zy4C+OGpamoFVy38MVBnE+IbbVYUew+OrCXaRkfj\" crossorigin=\"anonymous\"/><![CDATA[]]></script>\r\n";
            r += "<script src=\"https://cdn.jsdelivr.net/npm/bootstrap@4.5.3/dist/js/bootstrap.bundle.min.js\" integrity=\"sha384-ho+j7jyWK8fNQe+A12Hb8AhRq26LrZ/JpcUGGOn+Y7RsweNrtN/tE3MoK7ZeZDyx\" crossorigin=\"anonymous\"><![CDATA[]]></script>\r\n";
            r += "</body></html>\r\n";

            Client.Send(Encoding.UTF8.GetBytes(r));
            Client.Disconnect(true);
        }
    }
}
