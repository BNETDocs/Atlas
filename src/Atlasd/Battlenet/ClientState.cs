using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Atlasd.Battlenet
{
    class ClientState : IDisposable
    {
        public TcpClient Client { get; private set; }
        public NetworkStream ClientStream { get; private set; }
        public GameState GameState = null;
        public ProtocolType ProtocolType = ProtocolType.None;
        public System.Net.EndPoint RemoteEndPoint { get; private set; }

        protected byte[] ReceiveBuffer = new byte[0];
        protected byte[] SendBuffer = new byte[0];

        protected Frame BattlenetGameFrame = new Frame();

        public ClientState(TcpClient client)
        {
            Initialize(client);
            ThreadMain();
        }

        public void Dispose() /* part of IDisposable */
        {
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection forcefully closed by server");
            if (Client != null) Client.Close();

            lock (Common.ActiveClients) Common.ActiveClients.Remove(this);

            try
            {
                lock (ClientStream)
                {
                    ClientStream.Dispose();
                    ClientStream = null;
                }
            }
            catch (ArgumentNullException) { }
            catch (NullReferenceException) { }

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
        }

        protected void Initialize(TcpClient client)
        {
            lock (Common.ActiveClients) Common.ActiveClients.Add(this);

            Client = client;
            ClientStream = client.GetStream();
            RemoteEndPoint = client.Client.RemoteEndPoint;
            GameState = new GameState(this);

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, "TCP connection established");

            client.Client.NoDelay = true;

            if (client.Client.ReceiveBufferSize < 0xFFFF)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client, RemoteEndPoint, "Setting ReceiveBufferSize to [0xFFFF]");
                client.Client.ReceiveBufferSize = 0xFFFF;
            }

            if (client.Client.SendBufferSize < 0xFFFF)
            {
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client, RemoteEndPoint, "Setting SendBufferSize to [0xFFFF]");
                client.Client.SendBufferSize = 0xFFFF;
            }
        }

        public bool Invoke()
        {
            if (!Client.Connected || ClientStream == null || !ClientStream.CanWrite)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection lost");
                return false;
            }

            try
            {
                while (BattlenetGameFrame.Messages.Count > 0)
                {
                    if (!BattlenetGameFrame.Messages.Dequeue().Invoke(new MessageContext(this, Protocols.MessageDirection.ClientToServer))) return false;
                }
            }
            catch (GameProtocolViolationException ex)
            {
                var log_type = (ProtocolType) switch
                {
                    ProtocolType.Game => Logging.LogType.Client_Game,
                    ProtocolType.BNFTP => Logging.LogType.Client_BNFTP,
                    ProtocolType.Chat => Logging.LogType.Client_Chat,
                    ProtocolType.Chat_Alt1 => Logging.LogType.Client_Chat,
                    ProtocolType.Chat_Alt2 => Logging.LogType.Client_Chat,
                    _ => Logging.LogType.Client,
                };
                Logging.WriteLine(Logging.LogLevel.Warning, log_type, RemoteEndPoint, "Protocol violation exception!" + (ex.Message.Length > 0 ? " " + ex.Message : ""));

                return false;
            }

            return true;
        }

        public void Receive()
        {
            if (ProtocolType == ProtocolType.None) ReceiveProtocolType();
            ReceiveProtocol();
        }

        protected void ReceiveProtocolType()
        {
            if (!Client.Connected || ClientStream == null || !ClientStream.CanRead)
                throw new ClientException(this, "TCP connection lost");

            if (ProtocolType != ProtocolType.None) return;

            byte[] data = new byte[1];
            int size = ClientStream.Read(data); // Block until 1 byte is received
            if (size == 0) throw new ClientException(this, "TCP connection lost");

            ProtocolType = (ProtocolType)data[0];

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, string.Format("Set protocol type [0x{0:X2}] ({1})", (byte)ProtocolType, Common.ProtocolTypeName(ProtocolType)));
        }

        protected void ReceiveProtocol()
        {
            switch (ProtocolType)
            {
                case ProtocolType.Game:
                    ReceiveProtocolGame(); break;
                case ProtocolType.Chat:
                case ProtocolType.Chat_Alt1:
                case ProtocolType.Chat_Alt2:
                    ReceiveProtocolChat(); break;
                default:
                    throw new ProtocolNotSupportedException(ProtocolType, this, string.Format("Unsupported protocol type [0x{0:X2}]", (byte)ProtocolType));
            }
        }

        protected void ReceiveProtocolChat()
        {
            if (!Client.Connected || ClientStream == null || !(ClientStream.CanRead && ClientStream.CanWrite))
                throw new ClientException(this, "TCP connection lost");

            Send(System.Text.Encoding.ASCII.GetBytes("The chat gateway is currently unsupported on Atlasd.\r\n"));
            throw new ProtocolNotSupportedException(ProtocolType, this, "Unsupported protocol type [0x" + ((byte)ProtocolType).ToString("X2") + "]");
        }

        protected void ReceiveProtocolGame()
        {
            if (!Client.Connected || ClientStream == null || !(ClientStream.CanRead && ClientStream.CanWrite))
                throw new ClientException(this, "TCP connection lost");

            byte[] newBuffer;

            // Block for socket data transmission:
            byte[] data = new byte[0xFFFF];
            int size = ClientStream.Read(data);

            // Retest for connection after being blocked:
            if (!Client.Connected || ClientStream == null || !(ClientStream.CanRead && ClientStream.CanWrite))
                throw new ClientException(this, "TCP connection lost");

            // Append received data to previously received data
            if (size > 0)
            {
                newBuffer = new byte[ReceiveBuffer.Length + size];
                Buffer.BlockCopy(ReceiveBuffer, 0, newBuffer, 0, ReceiveBuffer.Length);
                Buffer.BlockCopy(data, 0, newBuffer, ReceiveBuffer.Length, size);
                ReceiveBuffer = newBuffer;
            }

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
                    throw new GameProtocolException(this, "Received unknown SID_0x" + messageId.ToString("X2") + " (" + messageLen.ToString() + " bytes)");
                }
            }
        }

        public bool Send(byte[] buffer)
        {
            if (ClientStream == null || !ClientStream.CanWrite)
            {
                Dispose();
                return false;
            }

            lock (ClientStream)
            {
                try
                {
                    ClientStream.Write(buffer);
                }
                catch (IOException ex)
                {
                    Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, ex.GetType().Name + " error encountered!" + (ex.Message.Length > 0 ? " " + ex.Message : ""));
                    Dispose();
                    return false;
                }
            }
            return true;
        }

        public void ThreadMain()
        {
            // Spawn a new thread to handle this connection ...
            new Thread(() =>
            {
                while (true) // Infinitely loop childSocketThread ...
                {
                    var bCloseConnection = true;

                    try
                    {
                        Receive();

                        if (!Invoke()) break;
                        
                        bCloseConnection = false;
                        continue;
                    }
                    catch (ClientException ex)
                    {
                        Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, ex.Message.Length > 0 ? ex.Message : null);
                    }
                    catch (SocketException ex)
                    {
                        Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection lost!" + (ex.Message.Length > 0 ? " " + ex.Message : ""));
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, ex.GetType().Name + " error encountered!" + (ex.Message.Length > 0 ? " " + ex.Message : ""));
                    }
                    finally
                    {
                        if (bCloseConnection) Dispose();
                    }

                    break;
                }
            }).Start();
        }
    }
}
