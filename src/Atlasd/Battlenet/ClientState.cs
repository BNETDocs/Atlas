using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atlasd.Battlenet
{
    class ClientState : IDisposable
    {
        public bool IsDisposing { get; private set; } = false;

        public GameState GameState { get; private set; }
        public Timer NullTimer { get; private set; }
        public Timer PingTimer { get; private set; }
        public ProtocolType ProtocolType { get; private set; }
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
                    if (GameState != null) GameState.Dispose();
                    GameState = null;
                }
            }
            catch (ArgumentNullException) { }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }

            IsDisposing = false;
        }

        protected void Initialize(Socket client)
        {
            lock (Common.ActiveClients) Common.ActiveClients.Add(this);

            GameState = null;
            NullTimer = null;
            PingTimer = null;
            ProtocolType = null;
            RemoteEndPoint = client.RemoteEndPoint;
            Socket = client;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, "TCP connection established");

            client.NoDelay = true;
            client.ReceiveTimeout = 500;
            client.SendTimeout = 500;

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

            Task.Run(() =>
            {
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
            });
        }

        void ProcessNullTimer(object state)
        {
            switch (ProtocolType.Type)
            {
                case ProtocolType.Types.Game:
                    {
                        new SID_NULL().Invoke(new MessageContext(this, Protocols.MessageDirection.ServerToClient));
                        break;
                    }
                case ProtocolType.Types.Chat:
                case ProtocolType.Types.Chat_Alt1:
                case ProtocolType.Types.Chat_Alt2:
                    {
                        Send(Encoding.ASCII.GetBytes($"{2000 + (ushort)MessageIds.SID_NULL} NULL\r\n"));
                        break;
                    }
                default:
                    throw new ProtocolNotSupportedException(ProtocolType.Type, this, $"Unsupported protocol type [0x{(byte)ProtocolType.Type:X2}]");
            }
        }

        void ProcessPingTimer(object state)
        {
            var clientState = state as ClientState;
            clientState.GameState.PingDelta = DateTime.Now;

            switch (ProtocolType.Type)
            {
                case ProtocolType.Types.Game:
                    {
                        new SID_PING().Invoke(new MessageContext(this, Protocols.MessageDirection.ServerToClient, new Dictionary<string, object>(){{ "token", GameState.PingToken }}));
                        break;
                    }
                case ProtocolType.Types.Chat:
                case ProtocolType.Types.Chat_Alt1:
                case ProtocolType.Types.Chat_Alt2:
                    {
                        Send(Encoding.ASCII.GetBytes($"{2000 + (ushort)MessageIds.SID_PING} PING\r\n"));
                        break;
                    }
                default:
                    throw new ProtocolNotSupportedException(ProtocolType.Type, this, $"Unsupported protocol type [0x{(byte)ProtocolType.Type:X2}]");
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

            if (ProtocolType == null) ReceiveProtocolType(e);
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
        }

        protected void ReceiveProtocolType(SocketAsyncEventArgs e)
        {
            if (ProtocolType != null) return;

            ProtocolType = new ProtocolType((ProtocolType.Types)ReceiveBuffer[0]);
            ReceiveBuffer = ReceiveBuffer[1..];

            if (ProtocolType.IsGame() || ProtocolType.IsChat())
            {
                GameState = new GameState(this);
                NullTimer = new Timer(ProcessNullTimer, this, 20000, 20000); // every 20 seconds send SID_NULL
                PingTimer = new Timer(ProcessPingTimer, this, 60000, 60000); // every 60 seconds send SID_PING
            }

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"Set protocol type [0x{(byte)ProtocolType.Type:X2}] ({ProtocolType})");
        }

        protected void ReceiveProtocol(SocketAsyncEventArgs e)
        {
            switch (ProtocolType.Type)
            {
                case ProtocolType.Types.Game:
                    ReceiveProtocolGame(e); break;
                case ProtocolType.Types.Chat:
                case ProtocolType.Types.Chat_Alt1:
                case ProtocolType.Types.Chat_Alt2:
                    ReceiveProtocolChat(e); break;
                default:
                    throw new ProtocolNotSupportedException(ProtocolType.Type, this, $"Unsupported protocol type [0x{(byte)ProtocolType.Type:X2}]");
            }
        }

        protected void ReceiveProtocolChat(SocketAsyncEventArgs e)
        {
            Send(System.Text.Encoding.ASCII.GetBytes("The chat gateway is currently unsupported on Atlasd.\r\n"));
            throw new ProtocolNotSupportedException(ProtocolType.Type, this, $"Unsupported protocol type [0x{(byte)ProtocolType.Type:X2}]");
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
                    ProtocolType.Types.Game => Logging.LogType.Client_Game,
                    ProtocolType.Types.BNFTP => Logging.LogType.Client_BNFTP,
                    ProtocolType.Types.Chat => Logging.LogType.Client_Chat,
                    ProtocolType.Types.Chat_Alt1 => Logging.LogType.Client_Chat,
                    ProtocolType.Types.Chat_Alt2 => Logging.LogType.Client_Chat,
                    ProtocolType.Types.IPC => Logging.LogType.Client_IPC,
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
