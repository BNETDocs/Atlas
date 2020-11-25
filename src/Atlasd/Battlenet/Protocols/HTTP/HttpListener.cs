using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Http, e.RemoteEndPoint, "HTTP listener socket error occurred!");
            }

            // Start the read loop on a new stack
            Task.Run(new HttpSession(e.AcceptSocket).ConnectedEvent);

            // Accept the next connection request
            StartAccept(e);
        }

        public void SetLocalEndPoint(IPEndPoint endp) /* part of IListener */
        {
            if (IsListening)
            {
                throw new InvalidOperationException();
            }

            LocalEndPoint = endp;
        }

        public void Start() /* part of IListener */
        {
            Stop();

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Starting HTTP listener on [{LocalEndPoint}]");

            Socket = new Socket(LocalEndPoint.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
            {
                ExclusiveAddressUse = true,
                NoDelay = Daemon.Common.TcpNoDelay,
                UseOnlyOverlappedIO = true,
            };
            Socket.Bind(LocalEndPoint);
            Socket.Listen(-1);

            StartAccept(null);
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            bool willRaiseEvent = Socket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
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
