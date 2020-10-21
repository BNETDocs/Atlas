using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Daemon;
using System;
using System.Net.Sockets;

namespace Atlasd.Battlenet
{
    class ClientState : IDisposable
    {
        public bool IsDisposing { get; private set; } = false;

        public GameState GameState = null;
        public ProtocolType ProtocolType = ProtocolType.None;
        public System.Net.EndPoint RemoteEndPoint { get; private set; }
        public Socket Socket { get; set; }

        protected byte[] ReceiveBuffer = new byte[0];
        protected byte[] SendBuffer = new byte[0];

        protected Frame BattlenetGameFrame = new Frame();

        public ClientState(Socket client)
        {
            Initialize(client);
        }

        public void Dispose() /* part of IDisposable */
        {
            if (IsDisposing) return;
            IsDisposing = true;

            if (Socket != null)
            {
                try
                {
                    Socket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception) { }
                Socket.Close();

                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection forcefully closed by server");
            }

            lock (Common.ActiveClients) Common.ActiveClients.Remove(this);

            try
            {
                lock (GameState)
                {
                    GameState.Dispose();
                    GameState = null;
                }
            }
            catch (ArgumentNullException) { }
            catch (NullReferenceException) { }

            IsDisposing = false;
        }

        protected void Initialize(Socket client)
        {
            lock (Common.ActiveClients) Common.ActiveClients.Add(this);

            Socket = client;
            RemoteEndPoint = client.RemoteEndPoint;
            GameState = null;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, "TCP connection established");

            client.NoDelay = true;

            if (client.ReceiveBufferSize < 0xFFFF)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client, RemoteEndPoint, "Setting ReceiveBufferSize to [0xFFFF]");
                client.ReceiveBufferSize = 0xFFFF;
            }

            if (client.SendBufferSize < 0xFFFF)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client, RemoteEndPoint, "Setting SendBufferSize to [0xFFFF]");
                client.SendBufferSize = 0xFFFF;
            }
        }

        private void Invoke(SocketAsyncEventArgs e)
        {
            var context = new MessageContext(this, Protocols.MessageDirection.ClientToServer);

            lock (BattlenetGameFrame.Messages)
            {
                while (BattlenetGameFrame.Messages.Count > 0)
                {
                    if (!BattlenetGameFrame.Messages.Dequeue().Invoke(context))
                    {
                        Dispose();
                        return;
                    }
                }
            }
        }

        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            if (!(e.SocketError == SocketError.Success && e.BytesTransferred > 0))
            {
                if (!IsDisposing && Socket != null)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"TCP connection lost");
                    Dispose();
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

            if (ProtocolType == ProtocolType.None) ReceiveProtocolType(e);
            ReceiveProtocol(e);
        }

        public void ProcessSend(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            if (e.SocketError != SocketError.Success)
            {
                if (!IsDisposing && Socket != null)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"TCP connection lost");
                    Dispose();
                }
                return;
            }

            e.SetBuffer(new byte[1024], 0, 1024);

            // read the next block of data send from the client
            bool willRaiseEvent = Socket.ReceiveAsync(e);
            if (!willRaiseEvent)
            {
                SocketIOCompleted(this, e);
            }
        }

        protected void ReceiveProtocolType(SocketAsyncEventArgs e)
        {
            if (ProtocolType != ProtocolType.None) return;

            ProtocolType = (ProtocolType)ReceiveBuffer[0];
            ReceiveBuffer = ReceiveBuffer[1..];

            if (ProtocolType == ProtocolType.Game ||
                ProtocolType == ProtocolType.Chat ||
                ProtocolType == ProtocolType.Chat_Alt1 ||
                ProtocolType == ProtocolType.Chat_Alt2)
            {
                GameState = new GameState(this);
            }

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, string.Format("Set protocol type [0x{0:X2}] ({1})", (byte)ProtocolType, Common.ProtocolTypeName(ProtocolType)));
        }

        protected void ReceiveProtocol(SocketAsyncEventArgs e)
        {
            switch (ProtocolType)
            {
                case ProtocolType.Game:
                    ReceiveProtocolGame(e); break;
                case ProtocolType.Chat:
                case ProtocolType.Chat_Alt1:
                case ProtocolType.Chat_Alt2:
                    ReceiveProtocolChat(e); break;
                default:
                    throw new ProtocolNotSupportedException(ProtocolType, this, string.Format("Unsupported protocol type [0x{0:X2}]", (byte)ProtocolType));
            }
        }

        protected void ReceiveProtocolChat(SocketAsyncEventArgs e)
        {
            Send(System.Text.Encoding.ASCII.GetBytes("The chat gateway is currently unsupported on Atlasd.\r\n"));
            throw new ProtocolNotSupportedException(ProtocolType, this, "Unsupported protocol type [0x" + ((byte)ProtocolType).ToString("X2") + "]");
        }

        protected void ReceiveProtocolGame(SocketAsyncEventArgs e)
        {
            byte[] newBuffer;

            while (ReceiveBuffer.Length > 0)
            {
                if (ReceiveBuffer.Length < 4) return; // Partial message header

                UInt16 messageLen = (UInt16)((ReceiveBuffer[3] << 8) + ReceiveBuffer[2]);

                if (ReceiveBuffer.Length < messageLen) return; // Partial message

                //byte messagePad = ReceiveBuffer[0];
                byte messageId = ReceiveBuffer[1];
                byte[] messageBuffer = new byte[messageLen - 4];
                Buffer.BlockCopy(ReceiveBuffer, 4, messageBuffer, 0, messageLen - 4);

                // Pop message off the receive buffer
                newBuffer = new byte[ReceiveBuffer.Length - messageLen];
                Buffer.BlockCopy(ReceiveBuffer, messageLen, newBuffer, 0, ReceiveBuffer.Length - messageLen);
                ReceiveBuffer = newBuffer;

                // Push message onto stack
                Message message = Message.FromByteArray(messageId, messageBuffer);

                if (message is Message)
                {
                    BattlenetGameFrame.Messages.Enqueue(message);
                    continue;
                }
                else
                {
                    throw new GameProtocolException(this, $"Received unknown SID_0x{messageId:X2} ({messageLen} bytes)");
                }
            }

            Invoke(e);
        }

        public void Send(byte[] buffer)
        {
            var e = new SocketAsyncEventArgs();
            e.Completed += new EventHandler<SocketAsyncEventArgs>(SocketIOCompleted);
            e.SetBuffer(buffer, 0, buffer.Length);
            e.UserToken = this;

            bool willRaiseEvent = Socket.SendAsync(e);
            if (!willRaiseEvent)
            {
                SocketIOCompleted(this, e);
            }
        }

        void SocketIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            var clientState = e.UserToken as ClientState;

            try
            {
                // determine which type of operation just completed and call the associated handler
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        ProcessSend(e);
                        break;
                    default:
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (GameProtocolViolationException ex)
            {
                var log_type = ex.ProtocolType switch
                {
                    ProtocolType.Game => Logging.LogType.Client_Game,
                    ProtocolType.BNFTP => Logging.LogType.Client_BNFTP,
                    ProtocolType.Chat => Logging.LogType.Client_Chat,
                    ProtocolType.Chat_Alt1 => Logging.LogType.Client_Chat,
                    ProtocolType.Chat_Alt2 => Logging.LogType.Client_Chat,
                    _ => Logging.LogType.Client,
                };
                Logging.WriteLine(Logging.LogLevel.Warning, log_type, clientState.RemoteEndPoint, "Protocol violation encountered!" + (ex.Message.Length > 0 ? $" {ex.Message}" : ""));
                clientState.Dispose();
            }
            catch (Exception ex)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, clientState.RemoteEndPoint, $"{ex.GetType().Name} error encountered!" + (ex.Message.Length > 0 ? $" {ex.Message}" : ""));
                clientState.Dispose();
            }
        }

        public void SocketIOCompleted_External(object sender, SocketAsyncEventArgs e)
        {
            var clientState = e.UserToken as ClientState;
            if (clientState != this)
            {
                throw new NotSupportedException();
            }

            SocketIOCompleted(sender, e);
        }
    }
}
