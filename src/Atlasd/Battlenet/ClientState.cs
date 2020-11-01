﻿using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.BNFTP;
using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Atlasd.Battlenet
{
    class ClientState : IDisposable
    {
        public BNFTPState BNFTPState;
        public bool Connected { get => Socket != null && Socket.Connected; }
        public bool IsDisposing { get; private set; } = false;

        public GameState GameState { get; private set; }
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

        public void Close()
        {
            Disconnect();
        }

        public void Disconnect(string reason = null)
        {
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection forcefully closed by server");

            // If reason was provided, send it to this client
            if (reason != null)
            {
                if (GameState != null)
                {
                    var r = reason.Length == 0 ? Resources.DisconnectedByAdmin : Resources.DisconnectedByAdminWithReason;

                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, GameState.ChannelFlags, GameState.Ping, GameState.OnlineName, r).WriteTo(this);
                }
            }

            // Close the GameState
            try
            {
                if (GameState != null)
                {
                    GameState.Close();
                    GameState = null;
                }
            }
            catch (ObjectDisposedException) { }

            // Remove this from ActiveClientStates
            lock (Common.ActiveClientStates) Common.ActiveClientStates.Remove(this);

            // Close the connection
            try
            {
                Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                if (!(ex is SocketException || ex is ObjectDisposedException)) throw;
            }
            finally
            {
                if (Socket != null)
                {
                    Socket.Close();
                }
            }
        }

        public void Dispose() /* part of IDisposable */
        {
            if (IsDisposing) return;
            IsDisposing = true;

            Disconnect();

            IsDisposing = false;
        }

        protected void Initialize(Socket client)
        {
            lock (Common.ActiveClientStates) Common.ActiveClientStates.Add(this);

            BNFTPState.Client = this;
            GameState = null;
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
            if (e.SocketError != SocketError.Success) return;

            var context = new MessageContext(this, Protocols.MessageDirection.ClientToServer);

            lock (BattlenetGameFrame.Messages)
            {
                while (BattlenetGameFrame.Messages.Count > 0)
                {
                    if (!BattlenetGameFrame.Messages.TryDequeue(out var msg))
                    {
                        Disconnect();
                        return;
                    }

                    if (!msg.Invoke(context))
                    {
                        Disconnect();
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
        }

        public void ReceiveAsync()
        {
            if (Socket == null) return;

            var readEventArgs = new SocketAsyncEventArgs();
            readEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketIOCompleted);
            readEventArgs.SetBuffer(new byte[1024], 0, 1024);
            readEventArgs.UserToken = this;

            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent;
            try
            {
                willRaiseEvent = Socket.ReceiveAsync(readEventArgs);
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

        protected void ReceiveProtocolType(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;
            if (ProtocolType != null) return;

            ProtocolType = new ProtocolType((ProtocolType.Types)ReceiveBuffer[0]);
            ReceiveBuffer = ReceiveBuffer[1..];

            if (ProtocolType.IsGame() || ProtocolType.IsChat())
            {
                GameState = new GameState(this);
            }

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"Set protocol type [0x{(byte)ProtocolType.Type:X2}] ({ProtocolType})");
        }

        protected void ReceiveProtocol(SocketAsyncEventArgs e)
        {
            switch (ProtocolType.Type)
            {
                case ProtocolType.Types.Game:
                    ReceiveProtocolGame(e); break;
                case ProtocolType.Types.BNFTP:
                    ReceiveProtocolBNFTP(e); break;
                case ProtocolType.Types.Chat:
                case ProtocolType.Types.Chat_Alt1:
                case ProtocolType.Types.Chat_Alt2:
                    ReceiveProtocolChat(e); break;
                default:
                    throw new ProtocolNotSupportedException(ProtocolType.Type, this, $"Unsupported protocol type [0x{(byte)ProtocolType.Type:X2}]");
            }
        }

        protected void ReceiveProtocolBNFTP(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;

            using var m = new MemoryStream(e.Buffer);
            using var r = new BinaryReader(m);

            var headerLength = r.ReadUInt16();
            var protocolVersion = r.ReadUInt16();

            switch (protocolVersion)
            {
                case 0x0100:
                    {
                        /**
                         * ## VERSION 1    <-- ##
                         * ## Client -> Server ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT16     Request Length
                         * UINT16     Protocol Version           0x100 (256)
                         * UINT32     Platform ID                See Product Identification
                         * UINT32     Product ID                 See Product Identification
                         * UINT32     Ad Banner ID               0 unless downloading an ad banner
                         * UINT32     Ad Banner File Extension   0 unless downloading an ad banner
                         * UINT32     File start position        For resuming an incomplete download
                         * FILETIME   Filetime
                         * STRING     Filename
                         */

                        /**
                         * ## VERSION 1    --> ##
                         * ## Server -> Client ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT16     Header Length              Does not include the file length
                         * UINT16     Type
                         * UINT32     File size
                         * UINT32     Ad Banner ID               0 unless downloading an ad banner
                         * UINT32     Ad Banner File Extension   0 unless downloading an ad banner
                         * FILETIME   Filetime
                         * STRING     Filename
                         * VOID       File data
                        */

                        break;
                    }
                case 0x0200:
                    {
                        /**
                         * ## VERSION 2    <-- ##
                         * ## Client -> Server ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT16     Request Length
                         * UINT16     Protocol Version           0x100 (256)
                         * UINT32     Platform ID                See Product Identification
                         * UINT32     Product ID                 See Product Identification
                         * UINT32     Ad Banner ID               0 unless downloading an ad banner
                         * UINT32     Ad Banner File Extension   0 unless downloading an ad banner
                         * UINT32     File start position        For resuming an incomplete download
                         * FILETIME   Filetime
                         * STRING     Filename
                         */

                        /**
                         * ## VERSION 2    --> ##
                         * ## Server -> Client ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT32     Server Token
                         */

                        /**
                         * ## VERSION 2    <-- ##
                         * ## Client -> Server ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT32     Starting position          Facilitates resuming
                         * FILETIME   Local filetime
                         * UINT32     Client Token
                         * UINT32     Key Length
                         * UINT32     Key's product value
                         * UINT32     Key's public value
                         * UINT32     Unknown (Always 0)
                         * UINT32 [5] CD key hash
                         * STRING     Filename
                         */

                        /**
                         * ## VERSION 2    --> ##
                         * ## Server -> Client ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT16     Header Length              Does not include the file length
                         * UINT32     File size
                         * UINT32     Ad Banner ID               0 unless downloading an ad banner
                         * UINT32     Ad Banner File Extension   0 unless downloading an ad banner
                         * FILETIME   Filetime
                         * STRING     Filename
                         * VOID       File data
                        */

                        break;
                    }
            }
        }

        protected void ReceiveProtocolChat(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;
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
            if (Socket == null) return;

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
            var clientState = e.UserToken as ClientState;

            try
            {
                // determine which type of operation just completed and call the associated handler
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        clientState.ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        clientState.ProcessSend(e);
                        break;
                    default:
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (GameProtocolViolationException ex)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, (Logging.LogType)ProtocolType.ProtocolTypeToLogType(ex.ProtocolType), clientState.RemoteEndPoint, "Protocol violation encountered!" + (ex.Message.Length > 0 ? $" {ex.Message}" : ""));
                clientState.Dispose();
            }
            catch (Exception ex)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, clientState.RemoteEndPoint, $"{ex.GetType().Name} error encountered!" + (ex.Message.Length > 0 ? $" {ex.Message}" : ""));
                clientState.Dispose();
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
            var clientState = e.UserToken as ClientState;
            if (clientState != this)
            {
                throw new NotSupportedException();
            }

            SocketIOCompleted(sender, e);
        }
    }
}
