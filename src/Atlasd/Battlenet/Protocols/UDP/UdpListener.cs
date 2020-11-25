using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
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

        public void Parse(byte[] datagram, EndPoint remoteEndpoint)
        {
            if (datagram.Length < 4)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received junk datagram ({datagram.Length} bytes < 4 bytes)");
                return;
            }

            try
            {
                using var m = new MemoryStream(datagram);
                using var r = new BinaryReader(m);

                var messageId = r.ReadUInt32();

                switch (messageId)
                {
                    case 0x00: // PKT_STORM
                        {
                            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received junk datagram [PKT_STORM] ({datagram.Length} bytes)");
                            break;
                        }
                    case 0x03: // PKT_CLIENTREQ
                        {
                            if (datagram.Length != 8)
                            {
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received echo request [PKT_CLIENTREQ] ({datagram.Length} bytes != 8 bytes)");
                            }
                            else
                            {
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received echo request [PKT_CLIENTREQ] ({datagram.Length} bytes); replying");
                                Socket.SendTo(datagram, remoteEndpoint);
                            }

                            break;
                        }
                    case 0x08: // PKT_CONNTEST
                        {
                            if (datagram.Length != 8)
                            {
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received UDP test [PKT_CONNTEST] ({datagram.Length} bytes != 8 bytes)");
                            }
                            else
                            {
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received UDP test [PKT_CONNTEST] ({datagram.Length} bytes)");
                                var code = new byte[] { 0x74, 0x65, 0x6E, 0x62 }; // Value "bnet" for SID_UDPPINGRESPONSE
                                Socket.SendTo(code, remoteEndpoint);
                            }

                            break;
                        }
                    case 0x09: // PKT_CONNTEST2
                        {
                            if (datagram.Length != 12)
                            {
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received UDP test [PKT_CONNTEST2] ({datagram.Length} bytes != 12 bytes)");
                            }
                            else
                            {
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received UDP test [PKT_CONNTEST2] ({datagram.Length} bytes)");
                                var code = new byte[] { 0x74, 0x65, 0x6E, 0x62 }; // Value "bnet" for SID_UDPPINGRESPONSE
                                Socket.SendTo(code, remoteEndpoint);
                            }

                            break;
                        }
                    default:
                        {
                            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received junk datagram ({datagram.Length} bytes)");
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_UDP, remoteEndpoint, $"{e.GetType().Name} error occurred while parsing UDP datagram");
            }
        }

        void ReceiveFromAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Client_UDP, e.RemoteEndPoint, $"Socket error occurred. Stopping UDP service.");
                Stop();
                return;
            }

            if (e.LastOperation != SocketAsyncOperation.ReceiveFrom) return;

            var bytes = e.Buffer[e.Offset..e.BytesTransferred];
            var endp = e.RemoteEndPoint;

            var buf = string.Empty; // buffer
            var pre = string.Empty; // preview

            for (var i = 0; i < bytes.Length; i++)
            {
                if (i % 16 == 0 && !string.IsNullOrEmpty(pre))
                {
                    buf += pre + Environment.NewLine;
                    pre = string.Empty;
                }

                buf += $"{bytes[i]:X2} ";
                pre += i > 32 && i < 127 ? (char)bytes[i] : '.';
            }
            buf += pre;

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, e.RemoteEndPoint, $"Received Datagram: {buf}");

            Parse(bytes, endp);

            // Start next read
            e.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            e.SetBuffer(new byte[2048], 0, 2048);
            bool willRaiseEvent = Socket.ReceiveFromAsync(e);
            if (!willRaiseEvent)
            {
                ReceiveFromAsyncCompleted(this, e);
            }
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

        public void Stop()
        {
            if (!IsListening) return;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Stopping UDP listener on [{Socket.LocalEndPoint}]");

            Socket.Close();
            Socket = null;
        }
    }
}
