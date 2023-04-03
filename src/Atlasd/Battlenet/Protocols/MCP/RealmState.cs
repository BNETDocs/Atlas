using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.BNFTP;
using Atlasd.Battlenet.Protocols.MCP;
using Atlasd.Battlenet.Protocols.MCP.Models;
//using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Atlasd.Battlenet
{
    class RealmState
    {
        public bool Connected { get => Socket != null && Socket.Connected; }
        public bool IsClosing { get; private set; } = false;

        public ClientState ClientState { get; set; }
        public Character ActiveCharacter { get; set; }
        public ProtocolType ProtocolType { get; private set; }
        public EndPoint RemoteEndPoint { get; private set; }
        public IPAddress RemoteIPAddress { get; private set; }
        public Socket Socket { get; set; }

        protected byte[] ReceiveBuffer = new byte[0];
        protected byte[] SendBuffer = new byte[0];

        protected Frame RealmGameFrame = new Frame();

        private const int HEADER_SIZE = 3;

        public RealmState(Socket client)
        {
            Initialize(client);
        }

        public void Close()
        {
            if (IsClosing) return;
            IsClosing = true;

            Disconnect();

            IsClosing = false;
        }

        public void Disconnect(string reason = null)
        {
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP realm connection forcefully closed by server");

            // Remove this from ActiveClientStates
            //if (!Common.ActiveClientStates.TryRemove(this.Socket, out _))
            //{
            //    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Failed to remove client state [{RemoteEndPoint}] from active client state cache");
            //}

            // Close the connection
            try
            {
                if (Socket != null && Socket.Connected) Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                if (!(ex is SocketException || ex is ObjectDisposedException)) throw;
            }
            finally
            {
                if (Socket != null) Socket.Close();
            }
        }

        protected void Initialize(Socket client)
        {
            //if (!Common.ActiveClientStates.TryAdd(client, this))
            //{
            //    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Failed to add client state [{this.Socket.RemoteEndPoint}] to active client state cache");
            //}

            RemoteEndPoint = client.RemoteEndPoint;
            RemoteIPAddress = (client.RemoteEndPoint as IPEndPoint).Address;
            Socket = client;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, "TCP realm connection established");

            client.NoDelay = Daemon.Common.TcpNoDelay;
            client.ReceiveTimeout = 500;
            client.SendTimeout = 500;

            if (client.ReceiveBufferSize < 0xFFFF)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client, RemoteEndPoint, "Setting realm ReceiveBufferSize to [0xFFFF]");
                client.ReceiveBufferSize = 0xFFFF;
            }

            if (client.SendBufferSize < 0xFFFF)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client, RemoteEndPoint, "Setting realm SendBufferSize to [0xFFFF]");
                client.SendBufferSize = 0xFFFF;
            }
        }

        private void Invoke(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;

            var context = new MessageContext(this, Protocols.MessageDirection.ClientToServer);

            while (RealmGameFrame.Messages.TryDequeue(out var msg))
            {
                if (!msg.Invoke(context))
                {
                    Disconnect();
                    return;
                }
            }
        }

        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            if (!(e.SocketError == SocketError.Success && e.BytesTransferred > 0))
            {
                if (!IsClosing && Socket != null)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"TCP realm connection lost");
                    Close();
                }
                return;
            }

            // Append received data to previously received data
            lock (ReceiveBuffer)
            {
                var newBuffer = new byte[ReceiveBuffer.Length + e.BytesTransferred];
                Buffer.BlockCopy(ReceiveBuffer, 0, newBuffer, 0, ReceiveBuffer.Length);
                Buffer.BlockCopy(e.Buffer, e.Offset, newBuffer, ReceiveBuffer.Length, e.BytesTransferred);
                ReceiveBuffer = newBuffer;
            }

            ReceiveProtocol(e);
        }

        public void ProcessSend(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            if (e.SocketError != SocketError.Success)
            {
                if (!IsClosing && Socket != null)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"TCP realm connection lost");
                    Close();
                }
                return;
            }
        }

        public void ReceiveAsync()
        {
            if (Socket == null || !Socket.Connected) return;

            var readEventArgs = new SocketAsyncEventArgs();
            readEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketIOCompleted);
            readEventArgs.SetBuffer(new byte[1024], 0, 1024);
            readEventArgs.UserToken = this;

            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent;
            try
            {
                willRaiseEvent = Socket != null && Socket.Connected && Socket.ReceiveAsync(readEventArgs);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (!willRaiseEvent)
            {
                SocketIOCompleted(this, readEventArgs);
            }
        }

        protected void ReceiveProtocol(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;

            if (ProtocolType == null) ReceiveProtocolType(e);
            ReceiveProtocolMCP(e);
        }

        protected void ReceiveProtocolType(SocketAsyncEventArgs e)
        {
            if (ProtocolType != null) return;

            ProtocolType = new ProtocolType((ProtocolType.Types)ReceiveBuffer[0]);
            ReceiveBuffer = ReceiveBuffer[1..];

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"Set realm protocol type [0x{(byte)ProtocolType.Type:X2}] ({ProtocolType})");
        }

        protected void ReceiveProtocolMCP(SocketAsyncEventArgs e)
        {
            byte[] newBuffer;

            while (ReceiveBuffer.Length > 0)
            {
                if (ReceiveBuffer.Length < HEADER_SIZE) return; // Partial message header

                UInt16 messageLen = (UInt16)((ReceiveBuffer[1] << 8) + ReceiveBuffer[0]);

                if (ReceiveBuffer.Length < messageLen) return; // Partial message

                byte messageId = ReceiveBuffer[2];
                byte[] messageBuffer = new byte[messageLen - HEADER_SIZE];
                Buffer.BlockCopy(ReceiveBuffer, HEADER_SIZE, messageBuffer, 0, messageLen - HEADER_SIZE);

                // Pop message off the receive buffer
                newBuffer = new byte[ReceiveBuffer.Length - messageLen];
                Buffer.BlockCopy(ReceiveBuffer, messageLen, newBuffer, 0, ReceiveBuffer.Length - messageLen);
                ReceiveBuffer = newBuffer;

                // Push message onto stack
                Message message = Message.FromByteArray(messageId, messageBuffer);

                if (message is Message)
                {
                    RealmGameFrame.Messages.Enqueue(message);
                    continue;
                }
                else
                {
                    throw new RealmProtocolException(ClientState, $"Received unknown MCP_0x{messageId:X2} ({messageLen} bytes)");
                }
            }

            Invoke(e);
        }

        public void Send(byte[] buffer)
        {
            if (Socket == null) return;
            if (!Socket.Connected) return;

            var e = new SocketAsyncEventArgs();
            e.Completed += new EventHandler<SocketAsyncEventArgs>(SocketIOCompleted);
            e.SetBuffer(buffer, 0, buffer.Length);
            e.UserToken = this;

            bool willRaiseEvent;
            try
            {
                willRaiseEvent = Socket.SendAsync(e);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (!willRaiseEvent)
            {
                SocketIOCompleted(this, e);
            }
        }

        void SocketIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            var realmState = e.UserToken as RealmState;

            try
            {
                // determine which type of operation just completed and call the associated handler
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        realmState.ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        realmState.ProcessSend(e);
                        break;
                    default:
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (GameProtocolViolationException ex)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, (Logging.LogType)ProtocolType.ProtocolTypeToLogType(ex.ProtocolType), realmState.RemoteEndPoint, "Protocol violation encountered!" + (ex.Message.Length > 0 ? $" {ex.Message}" : ""));
                realmState.Close();
            }
            catch (Exception ex)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, realmState.RemoteEndPoint, $"{ex.GetType().Name} error encountered!" + (ex.Message.Length > 0 ? $" {ex.Message}" : ""));
                realmState.Close();
            }
            finally
            {
                if (e.LastOperation == SocketAsyncOperation.Receive)
                {
                    Task.Run(() =>
                    {
                        ReceiveAsync();
                    });
                }
            }
        }

        public void SocketIOCompleted_External(object sender, SocketAsyncEventArgs e)
        {
            var realmState = e.UserToken as RealmState;
            if (realmState != this)
            {
                throw new NotSupportedException();
            }

            SocketIOCompleted(sender, e);
        }
    }
}
