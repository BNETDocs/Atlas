using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Atlasd.Battlenet.Protocols.Udp
{
    class UdpListener : IDisposable, IListener
    {
        public bool IsListening { get => Socket != null; }
        public IPEndPoint LocalEndPoint { get; protected set; }
        public Socket Socket { get; protected set; }

        public UdpListener(IPEndPoint endp)
        {
            SetLocalEndPoint(endp);
        }

        public void Close()
        {
            Stop();
        }

        public void Dispose() /* part of IDisposable */
        {
            Close();
        }

        public void SetLocalEndPoint(IPEndPoint endp)
        {
            if (IsListening)
            {
                throw new InvalidOperationException();
            }

            LocalEndPoint = endp;
        }

        public void Start()
        {
            Stop();

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Starting UDP listener on [{LocalEndPoint}]");

            Socket = new Socket(SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            Socket.Bind(LocalEndPoint);

            var e = new SocketAsyncEventArgs();
            e.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveFromAsyncCompleted);
            e.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            e.SetBuffer(new byte[2048], 0, 2048);

            bool willRaiseEvent = Socket.ReceiveFromAsync(e);
            if (!willRaiseEvent)
            {
                ReceiveFromAsyncCompleted(this, e);
            }
        }

        void ReceiveFromAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation != SocketAsyncOperation.ReceiveFrom) return;

            var buf = string.Empty; // buffer
            var pre = string.Empty; // preview

            for (var i = 0; i < e.BytesTransferred; i++)
            {
                if (i % 16 == 0 && !string.IsNullOrEmpty(pre))
                {
                    buf += pre + Environment.NewLine;
                    pre = string.Empty;
                }

                buf += $"{e.Buffer[i]:X2} ";
                pre += i > 32 && i < 127 ? (char)e.Buffer[i] : '.';
            }

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, $"Received UDP datagram:{Environment.NewLine}{buf}");
        }

        public void Stop()
        {
            if (!IsListening) return;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Stopping UDP listener on [{Socket.LocalEndPoint}]");

            Socket.Close();
            Socket = null;
        }
    }
}
