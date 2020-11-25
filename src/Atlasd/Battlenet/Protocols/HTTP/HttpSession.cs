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

            string r = "";

            r += "HTTP/1.0 503 Service Unavailable\r\n";
            r += "Connection: close\r\n";
            r += "Content-Type: text/html;charset=utf-8\r\n";
            r += $"Date: {DateTime.Now.ToUniversalTime():ddd, dd MMM yyyy HH:mm:ss} GMT\r\n";
            r += $"Server: {server}\r\n";
            r += "\r\n";
            r += "<!DOCTYPE html>\r\n<html lang=\"en\"><head><title>Atlas</title></head><body>This HTTP endpoint is extremely fragile. Please be gentle.</body></html>\r\n";

            Client.Send(Encoding.UTF8.GetBytes(r));
            Client.Disconnect(true);
        }
    }
}
