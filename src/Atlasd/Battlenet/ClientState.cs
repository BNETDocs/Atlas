using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.BNFTP;
using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
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
    class ClientState
    {
        public BNFTPState BNFTPState;
        public bool Connected { get => Socket != null && Socket.Connected; }
        public bool IsClosing { get; private set; } = false;

        public GameState GameState { get; private set; }
        public ProtocolType ProtocolType { get; private set; }
        public EndPoint RemoteEndPoint { get; private set; }
        public IPAddress RemoteIPAddress { get; private set; }
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
            if (IsClosing) return;
            IsClosing = true;

            Disconnect();

            IsClosing = false;
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
                }
            }
            catch (ObjectDisposedException) { }
            finally
            {
                GameState = null;
            }

            // Remove this from ActiveClientStates
            if (!Common.ActiveClientStates.TryRemove(this.Socket, out _))
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Failed to remove client state [{RemoteEndPoint}] from active client state cache");
            }

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
            if (!Common.ActiveClientStates.TryAdd(client, this))
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Failed to add client state [{this.Socket.RemoteEndPoint}] to active client state cache");
            }

            BNFTPState = null;
            GameState = null;
            ProtocolType = null;
            RemoteEndPoint = client.RemoteEndPoint;
            RemoteIPAddress = (client.RemoteEndPoint as IPEndPoint).Address;
            Socket = client;

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, "TCP connection established");

            client.NoDelay = Daemon.Common.TcpNoDelay;
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

            while (BattlenetGameFrame.Messages.TryDequeue(out var msg))
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
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"TCP connection lost");
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

            if (ProtocolType == null) ReceiveProtocolType(e);
            ReceiveProtocol(e);
        }

        public void ProcessSend(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            if (e.SocketError != SocketError.Success)
            {
                if (!IsClosing && Socket != null)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"TCP connection lost");
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

        protected void ReceiveProtocolType(SocketAsyncEventArgs e)
        {
            if (ProtocolType != null) return;

            ProtocolType = new ProtocolType((ProtocolType.Types)ReceiveBuffer[0]);
            ReceiveBuffer = ReceiveBuffer[1..];

            if (ProtocolType.IsGame() || ProtocolType.IsChat())
            {
                GameState = new GameState(this);
            }
            else if (ProtocolType.IsBNFTP())
            {
                BNFTPState = new BNFTPState(this);
            }

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"Set protocol type [0x{(byte)ProtocolType.Type:X2}] ({ProtocolType})");

            if (ProtocolType.IsChat())
            {
                GameState.Platform = Platform.PlatformCode.Windows;
                GameState.Product = Product.ProductCode.Chat;

                Send(Encoding.UTF8.GetBytes($"Connection from [{RemoteEndPoint}]{Common.NewLine}"));
                Send(Encoding.UTF8.GetBytes($"Enter your login name and password.{Common.NewLine}"));
            }
        }

        protected void ReceiveProtocol(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;

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
            if (ReceiveBuffer.Length == 0) return;
            BNFTPState.Receive(ReceiveBuffer);
        }

        protected void ReceiveProtocolChat(SocketAsyncEventArgs e)
        {
            string text;
            try
            {
                text = Encoding.UTF8.GetString(ReceiveBuffer);
            }
            catch (DecoderFallbackException)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Chat, RemoteEndPoint, "Failed to decode UTF-8 text");
                Disconnect("Failed to decode UTF-8 text");
                return;
            }

            // Mix alternate platform's new lines into our easily parsable NewLine constant:
            //text = text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Common.NewLine);

            while (text.Length > 0)
            {
                if (!text.Contains(Common.NewLine)) break; // Need more data from client

                var pos = text.IndexOf(Common.NewLine);
                ReceiveBuffer = ReceiveBuffer[(pos + Common.NewLine.Length)..];
                var line = text.Substring(0, pos);
                text = text[(line.Length + Common.NewLine.Length)..];

                if (GameState.ActiveAccount == null && string.IsNullOrEmpty(GameState.Username) && !string.IsNullOrEmpty(line) && line[0] == 0x04)
                {
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Chat, "Client sent login byte [0x04]");
                    line = line[1..];

                    Send(Encoding.UTF8.GetBytes("Username: "));
                    GameState.Username = null;
                }

                if (GameState.ActiveAccount == null && string.IsNullOrEmpty(GameState.Username) && !string.IsNullOrEmpty(line))
                {
                    GameState.Username = line;
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Chat, $"Client set username to [{GameState.Username}]");

                    Send(Encoding.UTF8.GetBytes("Password: "));
                    continue;
                }

                if (GameState.ActiveAccount == null)
                {
                    var autoAccountCreate = Settings.GetBoolean(new string[] { "battlenet", "emulation", "chat_gateway", "auto_account_create" }, false);
                    var inPasswordHash = MBNCSUtil.XSha1.CalculateHash(Encoding.UTF8.GetBytes(line.ToLower()));
                    line = string.Empty; // prevent echoing password as a message if successfully authenticated

                    if (!Common.AccountsDb.TryGetValue(GameState.Username, out Account account) || account == null)
                    {
                        if (!autoAccountCreate)
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Chat, "Client sent non-existent username");
                            Send(Encoding.UTF8.GetBytes($"Incorrect username/password.{Common.NewLine}"));
                            continue;
                        }
                    }

                    if (autoAccountCreate && account == null)
                    {
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Chat, $"Creating account [{GameState.Username}] automatically for chat gateway client");
                        Account.CreateStatus status = Account.TryCreate(GameState.Username, inPasswordHash, out account);
                        if (account == null || status != Account.CreateStatus.Success)
                        {
                            var message = "Incorrect username/password";
                            switch(status)
                            {
                                case Account.CreateStatus.AccountExists:
                                    message = "Account already exists"; break;
                                case Account.CreateStatus.LastCreateInProgress:
                                    message = "Last create in progress"; break;
                                case Account.CreateStatus.UsernameAdjacentPunctuation:
                                    message = "Username has adjacent punctuation"; break;
                                case Account.CreateStatus.UsernameBannedWord:
                                    message = "Username contains a banned word"; break;
                                case Account.CreateStatus.UsernameInvalidChars:
                                    message = "Username contains an invalid character"; break;
                                case Account.CreateStatus.UsernameShortAlphanumeric:
                                    message = "Username contains too few alphanumeric characters"; break;
                                case Account.CreateStatus.UsernameTooManyPunctuation:
                                    message = "Username contains too many punctuation characters"; break;
                                case Account.CreateStatus.UsernameTooShort:
                                    message = "Username is too short"; break;
                            }

                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Chat, $"[{message}]");
                            Send(Encoding.UTF8.GetBytes($"{message}.{Common.NewLine}"));
                            continue;
                        }
                        else
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Chat, $"Created account [{account.Get(Account.UsernameKey, GameState.Username)}] automatically for chat gateway client");
                        }
                    }

                    var dbPasswordHash = (byte[])account.Get(Account.PasswordKey, new byte[20]);
                    if (!inPasswordHash.SequenceEqual(dbPasswordHash))
                    {
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Chat, $"Incorrect password for account [{account.Get(Account.UsernameKey, GameState.Username)}]");
                        Send(Encoding.UTF8.GetBytes($"Incorrect username/password.{Common.NewLine}"));
                        continue;
                    }

                    var flags = (Account.Flags)account.Get(Account.FlagsKey, Account.Flags.None);
                    if ((flags & Account.Flags.Closed) != 0)
                    {
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Chat, $"Account [{account.Get(Account.UsernameKey, GameState.Username)}] is closed");
                        Send(Encoding.UTF8.GetBytes($"Account closed.{Common.NewLine}"));
                        continue;
                    }

                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Chat, $"Successfully authenticated into account [{account.Get(Account.UsernameKey, GameState.Username)}]");

                    lock (GameState)
                    {
                        GameState.ActiveAccount = account;
                        GameState.LastLogon = (DateTime)account.Get(Account.LastLogonKey, DateTime.Now);

                        account.Set(Account.IPAddressKey, RemoteEndPoint.ToString().Split(":")[0]);
                        account.Set(Account.LastLogonKey, DateTime.Now);
                        account.Set(Account.PortKey, RemoteEndPoint.ToString().Split(":")[1]);

                        var serial = 1;
                        var onlineName = GameState.Username;
                        while (!Common.ActiveAccounts.TryAdd(onlineName, account)) onlineName = $"{GameState.Username}#{++serial}";
                        GameState.OnlineName = onlineName;

                        GameState.Username = (string)account.Get(Account.UsernameKey, GameState.Username);
                        GameState.Statstring = new byte[1];
                    }

                    if (!Battlenet.Common.ActiveGameStates.TryAdd(GameState.OnlineName, GameState))
                    {
                        Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Client_Chat, RemoteEndPoint, $"Failed to add game state to active game state cache");
                        account.Set(Account.FailedLogonsKey, ((UInt32)account.Get(Account.FailedLogonsKey, (UInt32)0)) + 1);
                        Battlenet.Common.ActiveAccounts.TryRemove(GameState.OnlineName, out _);
                        Send(Encoding.UTF8.GetBytes($"Incorrect username/password.{Common.NewLine}"));
                        continue;
                    }

                    using var m1 = new MemoryStream(128);
                    using var w1 = new BinaryWriter(m1);
                    {
                        w1.Write(GameState.OnlineName);
                        w1.Write(GameState.Statstring);

                        new SID_ENTERCHAT(m1.ToArray()).Invoke(new MessageContext(this, Protocols.MessageDirection.ClientToServer,
                            new Dictionary<string, dynamic>{{ "username", GameState.Username }, { "statstring", GameState.Statstring }})
                        );
                    }

                    using var m2 = new MemoryStream(128);
                    using var w2 = new BinaryWriter(m2);
                    {
                        w2.Write((UInt32)SID_JOINCHANNEL.Flags.First);
                        w2.Write(Product.ProductChannelName(GameState.Product));

                        new SID_JOINCHANNEL(m2.ToArray()).Invoke(new MessageContext(this, Protocols.MessageDirection.ClientToServer));
                    }
                }

                if (string.IsNullOrEmpty(line)) continue;

                using var m3 = new MemoryStream(1 + Encoding.UTF8.GetByteCount(line));
                using var w3 = new BinaryWriter(m3);
                {
                    w3.Write(line);

                    new SID_CHATCOMMAND(m3.ToArray()).Invoke(new MessageContext(this, Protocols.MessageDirection.ClientToServer));
                }
            }
        }

        protected void ReceiveProtocolGame(SocketAsyncEventArgs e)
        {
            byte[] newBuffer;

            while (ReceiveBuffer.Length > 0)
            {
                if (ReceiveBuffer.Length < 4) return; // Partial message header

                UInt16 messageLen = (UInt16)((ReceiveBuffer[3] << 8) + ReceiveBuffer[2]);

                if (ReceiveBuffer.Length < messageLen) return; // Partial message

                //byte messagePad = ReceiveBuffer[0]; // This is checked in the Message.FromByteArray() call.
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
                clientState.Close();
            }
            catch (Exception ex)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, clientState.RemoteEndPoint, $"{ex.GetType().Name} error encountered!" + (ex.Message.Length > 0 ? $" {ex.Message}" : ""));
                clientState.Close();
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
