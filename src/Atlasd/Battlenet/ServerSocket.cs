﻿using Atlasd.Daemon;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Atlasd.Battlenet
{
    class ServerSocket : IDisposable
    {
        private bool IsDisposing = false;
        public bool IsListening { get; private set; }
        public IPEndPoint LocalEndPoint { get; private set; }
        public Socket Socket { get; private set; }

        public ServerSocket()
        {
            IsListening = false;
            LocalEndPoint = null;
            Socket = null;
        }

        public ServerSocket(IPEndPoint localEndPoint)
        {
            IsListening = false;
            Socket = null;
            SetLocalEndPoint(localEndPoint);
        }

        public void Dispose() /* part of IDisposable */
        {
            if (IsDisposing) return;
            IsDisposing = true;

            if (IsListening)
            {
                Stop();
            }

            if (Socket != null)
            {
                Socket = null;
            }

            if (LocalEndPoint != null)
            {
                LocalEndPoint = null;
            }

            IsDisposing = false;
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            var clientState = new ClientState(e.AcceptSocket);

            // Start the read loop on a new stack
            Task.Run(() =>
            {
                clientState.ReceiveAsync();
            });

            // Accept the next connection request
            StartAccept(e);
        }

        public void SetLocalEndPoint(IPEndPoint localEndPoint)
        {
            if (IsListening)
            {
                throw new NotSupportedException("Cannot set LocalEndPoint while socket is listening");
            }

            LocalEndPoint = localEndPoint;

            if (Socket != null)
            {
                Socket.Close();
            }

            Socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
            {
                ExclusiveAddressUse = true,
                NoDelay = Daemon.Common.TcpNoDelay,
                UseOnlyOverlappedIO = true,
            };
        }

        public void Start(int backlog = 100)
        {
            if (IsListening)
            {
                Stop();
            }

            if (LocalEndPoint == null)
            {
                throw new NullReferenceException("LocalEndPoint must be set to an instance of IPEndPoint");
            }

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Starting TCP listener on [{LocalEndPoint}]");

            Socket.Bind(LocalEndPoint);
            Socket.Listen(backlog);
            IsListening = true;

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

        public void Stop()
        {
            if (!IsListening) return;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Stopping TCP listener on [{Socket.LocalEndPoint}]");

            Socket.Close();
            Socket = null;

            IsListening = false;
        }
    }
}
