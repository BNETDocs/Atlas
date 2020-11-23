using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Http
{
    class HttpListener : IDisposable, IListener
    {
        public bool IsListening { get => Socket != null; } /* part of IListener */
        public IPEndPoint LocalEndPoint { get; protected set; } /* part of IListener */
        public Socket Socket { get; protected set; } /* part of IListener */

        public HttpListener(IPEndPoint endp)
        {
            SetLocalEndPoint(endp);
        }

        public void Close() /* part of IListener */
        {
            Stop();
        }

        public void Dispose() /* part of IDisposable */
        {
            Close();
        }

        public void SetLocalEndPoint(IPEndPoint endp) /* part of IListener */
        {
            if (IsListening)
            {
                throw new InvalidOperationException();
            }

            LocalEndPoint = endp;
        }

        static void SocketAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Http, e.RemoteEndPoint, "Listener socket error occurred!");
            }

            var client = e.AcceptSocket;

            var m = "HTTP/1.0 500 Internal Server Error\r\nConnection: close\r\nContent-Type: text/html;charset=utf-8\r\n\r\n<!DOCTYPE html>\r\n<html lang=\"en\"><head><title>Atlas</title></head><body>This HTTP endpoint is extremely fragile. Please be gentle.</body></html>\r\n";
            client.Send(Encoding.UTF8.GetBytes(m));
            client.Disconnect(true);
        }

        public void Start() /* part of IListener */
        {
            Stop();

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Starting HTTP listener on [{LocalEndPoint}]");

            Socket = new Socket(SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            Socket.Bind(LocalEndPoint);
            Socket.Listen(-1);

            var e = new SocketAsyncEventArgs();
            e.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncCompleted);

            bool willRaiseEvent = Socket.AcceptAsync(e);
            if (!willRaiseEvent)
            {
                SocketAsyncCompleted(this, e);
            }
        }

        public void Stop() /* part of IListener */
        {
            if (!IsListening) return;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Stopping HTTP listener on [{Socket.LocalEndPoint}]");

            Socket.Close();
            Socket = null;
        }
    }
}
