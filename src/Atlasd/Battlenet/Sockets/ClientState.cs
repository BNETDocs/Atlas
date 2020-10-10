using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Daemon;
using System;
using System.Net.Sockets;

namespace Atlasd.Battlenet.Sockets
{
    class ClientState
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
        }

        public void Close()
        {
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection forcefully closed by server");
            if (Client != null) Client.Close();
            if (GameState == null) return;

            if (GameState.ActiveAccount != null)
            {
                lock (GameState.ActiveAccount) {
                    GameState.ActiveAccount.Set(Account.LastLogoffKey, DateTime.Now);

                    var timeLogged = (UInt32)GameState.ActiveAccount.Get(Account.TimeLoggedKey);
                    var diff = DateTime.Now - GameState.ConnectedTimestamp;
                    timeLogged += (UInt32)Math.Round(diff.TotalSeconds);
                    GameState.ActiveAccount.Set(Account.TimeLoggedKey, timeLogged);

                    var username = (string)GameState.ActiveAccount.Get(Account.UsernameKey);
                    if (Common.ActiveAccounts.ContainsKey(username)) Common.ActiveAccounts.Remove(username);
                }
            }

            if (GameState.ActiveChannel != null)
            {
                GameState.ActiveChannel.RemoveUser(GameState);
            }
        }

        protected void Initialize(TcpClient client)
        {
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
            if (!Client.Connected || !Client.Client.Poll(0, System.Net.Sockets.SelectMode.SelectWrite))
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection lost");
                return false;
            }

            try
            {
                while (BattlenetGameFrame.Messages.Count > 0)
                {
                    var message = BattlenetGameFrame.Messages.Pop();
                    if (!message.Invoke(new MessageContext(this, Protocols.MessageDirection.ClientToServer))) return false;
                }
            }
            catch (ProtocolViolationException ex)
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

        public bool Receive()
        {
            if (!Client.Connected)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection lost");
                return false;
            }

            if (ProtocolType == ProtocolType.None)
            {
                byte[] data = new byte[1];
                int size = ClientStream.Read(data);
                if (size == 0) return false;

                ProtocolType = (ProtocolType)data[0];

                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, "Set protocol [" + Common.ProtocolTypeName(ProtocolType) + "]");
            }

            if (!Client.Connected)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection lost");
                return false;
            }

            switch (ProtocolType)
            {
                case ProtocolType.Game:
                    {
                        byte[] newBuffer;

                        while (ClientStream.DataAvailable)
                        {
                            byte[] data = new byte[0xFFFF];
                            int size = ClientStream.Read(data);

                            // Append received data to previously received data
                            if (size > 0)
                            {
                                newBuffer = new byte[ReceiveBuffer.Length + size];
                                Buffer.BlockCopy(ReceiveBuffer, 0, newBuffer, 0, ReceiveBuffer.Length);
                                Buffer.BlockCopy(data, 0, newBuffer, ReceiveBuffer.Length, size);
                                ReceiveBuffer = newBuffer;
                            }
                        }

                        if (!Client.Connected)
                        {
                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection lost");
                            return false;
                        }

                        while (ReceiveBuffer.Length > 0) {
                            if (ReceiveBuffer.Length < 4) return true; // Partial message header

                            UInt16 messageLen = (UInt16)((ReceiveBuffer[3] << 8) + ReceiveBuffer[2]);

                            if (ReceiveBuffer.Length < messageLen) return true; // Partial message

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
                                BattlenetGameFrame.Messages.Push(message);
                                continue;
                            } else
                            {
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, RemoteEndPoint, "Received unknown SID_0x" + messageId.ToString("X2") + " (" + messageLen.ToString() + " bytes)");
                                return false;
                            }
                        }

                        return true;
                    }
                case ProtocolType.Chat:
                case ProtocolType.Chat_Alt1:
                case ProtocolType.Chat_Alt2:
                    {
                        Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Chat, RemoteEndPoint, "Unsupported protocol [0x" + ((byte)ProtocolType).ToString("X2") + "]");
                        string message = "The chat gateway is currently unsupported on Atlasd.\r\n";
                        Client.Client.Send(System.Text.Encoding.ASCII.GetBytes(message));
                        return false;
                    }
                default:
                    {
                        Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "Unsupported protocol [0x" + ((byte)ProtocolType).ToString("X2") + "]");
                        return false;
                    }
            }
        }

        public bool Send(byte[] buffer)
        {
            if (ClientStream == null || !ClientStream.CanWrite)
            {
                Close();
                return false;
            }

            ClientStream.Write(buffer);
            return true;
        }
    }
}
