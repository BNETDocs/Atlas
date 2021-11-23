using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.BNFTP;
using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
                }
            }
            catch (ObjectDisposedException) { }
            finally
            {
                GameState = null;
            }

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

            BNFTPState = null;
            GameState = null;
            ProtocolType = null;
            RemoteEndPoint = client.RemoteEndPoint;
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

            // Fill the lineBuffer
            int indexCr, indexLf, indexCount, currentPos;
            string CurrentLine;
            while (text.Contains(Convert.ToChar(13)) || text.Contains(Convert.ToChar(10)))
            {
                // Topaz uses carriagereturn's (cr x3 x4 name cr pass cr [data cr])
                // UltimateBot (crlf, x3 x4 crlf name crlf pass crlf [data crlf])
                // MiniChat (x3 x4 crlf name crlf pass crlf [data crlf])
                // 
                // there was a few that used a similar data set to topaz where it wasent cr's but was lf's
                // and there is likely one's that used a mix of both.
                // pretty sure there was a login in the form of (x3 name crlf x4 pass crlf) aswell
                // 

                // need to check for mixing
                indexCr = text.IndexOf(Convert.ToChar(13)); // since vbLf is busted vbCr probably isnt far behind that.
                indexLf = text.IndexOf(Convert.ToChar(10)); // apparently vbLf is busted, but thats ok

                if (indexCr > -1 && indexLf > -1)
                {
                    // Are they side by side
                    if (indexCr < indexLf)
                    {
                        if ((indexLf - indexCr) == 1)
                            // theyre side by side
                            indexCount = 2;
                        else
                            // theyre not side by side
                            indexCount = 1;
                        currentPos = indexCr;
                    }
                    else
                    {
                        if ((indexCr - indexLf) == 1)
                            // theyre side by side
                            indexCount = 2;
                        else
                            // theyre not side by side
                            indexCount = 1;
                        currentPos = indexLf;
                    }
                }
                else
                {
                    // There was only 1 instance of either carriagereturn or linefeed
                    if (indexCr > -1)
                        currentPos = indexCr;
                    else
                        currentPos = indexLf;
                    indexCount = 1;
                }

                // currentPos = location of either the cr or the lf or both if side by side
                // indexCount = length of the delimiter if the cr and the lf are side by side then its 2 otherwise its 1
                // Text holds the pre-processed UTF8 ReceiveBuffer
                // ReceiveBuffer is moved to the next line
                ReceiveBuffer = ReceiveBuffer.Skip(currentPos + indexCount).ToArray();
                // CurrentLine is the first line 
                CurrentLine = text.Substring(0, currentPos);

                // was the line empty, and is there still data in the text buf
                if (CurrentLine == "")
                {
                    // move the text forward and push us back to the top of the while loop
                    text = text.Substring(currentPos + indexCount);
                    if (text == "")
                        // This was required for UB 4.13
                        Send(Encoding.UTF8.GetBytes($"Enter your Account name: {Common.NewLine}"));
                    continue;
                }

                // if GameState.ActiveAccount is nothing then the login process hasent completed yet
                if (GameState.ActiveAccount == null)
                {
                    // text holds the current line of data first order of buisness we are looking for
                    // 0x3 and 0x4, since the login could have come from Topaz or UltimateBot
                    if (CurrentLine[0] == Convert.ToChar(0x3))
                        CurrentLine = CurrentLine.Substring(1);
                    // Because the line may actually be empty check the length aswell, we will get this
                    // on the next pass. the above we know for sure is not empty.
                    if (CurrentLine.Length >= 1 && CurrentLine[0] == Convert.ToChar(0x4))
                    {
                        CurrentLine = CurrentLine.Substring(1);
                        // 
                        Send(Encoding.UTF8.GetBytes($"Enter your Account name: {Common.NewLine}"));
                    }
                    // At this point the CurrentLine could be empty again, if it is push the text forward again
                    if (CurrentLine == "")
                    {
                        // move the text forward and push us back to the top of the while loop
                        text = text.Substring(currentPos + indexCount);
                        continue;
                    }

                    // Second order of work we're looking for the UserName
                    if (string.IsNullOrEmpty(GameState.Username))
                    {
                        Send(Encoding.UTF8.GetBytes($"Username: {CurrentLine}{Common.NewLine}"));
                        GameState.Username = CurrentLine;
                        Send(Encoding.UTF8.GetBytes($"Enter your Account pass: {Common.NewLine}"));
                        text = text.Substring(currentPos + indexCount);
                        continue;
                    }

                    // Third we are looking for the Password to process the account forward
                    var inPasswordHash = MBNCSUtil.XSha1.CalculateHash(Encoding.UTF8.GetBytes(CurrentLine));
                    Common.AccountsDb.TryGetValue(GameState.Username, out var varAccount);

                    if (varAccount == null)
                    {
                        GameState.Username = null;
                        Send(Encoding.UTF8.GetBytes($"Incorrect username/password.{Common.NewLine}"));
                        return;
                    }

                    var dbPasswordHash = (byte[])varAccount.Get(Account.PasswordKey, new byte[20]);

                    if (!inPasswordHash.SequenceEqual(dbPasswordHash))
                    {
                        GameState.Username = null;
                        Send(Encoding.UTF8.GetBytes($"Incorrect username/password.{Common.NewLine}"));
                        return;
                    }

                    var flags = (Account.Flags)varAccount.Get(Account.FlagsKey, Account.Flags.None);

                    if ((flags & Account.Flags.Closed) != 0)
                    {
                        GameState.Username = null;
                        Send(Encoding.UTF8.GetBytes($"Account closed.{Common.NewLine}"));
                        return;
                    }

                    GameState.ActiveAccount = varAccount;
                    GameState.LastLogon = (DateTime)varAccount.Get(Account.LastLogonKey, DateTime.Now);
                    varAccount.Set(Account.IPAddressKey, RemoteEndPoint.ToString().Split(":")[0]);
                    varAccount.Set(Account.LastLogonKey, DateTime.Now);
                    varAccount.Set(Account.PortKey, RemoteEndPoint.ToString().Split(":")[1]);

                    lock (Common.ActiveAccounts)
                    {
                        var locSerial = 1;
                        var locOnlineName = GameState.Username;

                        while (Common.ActiveAccounts.ContainsKey(locOnlineName.ToLower()))
                            locOnlineName = $"{GameState.Username}#{System.Threading.Interlocked.Increment(ref locSerial)}";

                        GameState.OnlineName = locOnlineName;
                        Common.ActiveAccounts.Add(locOnlineName.ToLower(), varAccount);
                    }

                    GameState.Username = System.Convert.ToString(varAccount.Get(Account.UsernameKey, GameState.Username));

                    lock (Common.ActiveGameStates)
                        Common.ActiveGameStates.Add(GameState.OnlineName.ToLower(), GameState);

                    Send(Encoding.UTF8.GetBytes($"Connection from [{RemoteEndPoint}]{Common.NewLine}"));
                    GameState.GenerateStatstring();

                    // [Telnet SID_ENTERCHAT] = "2010 NAME {GameState.OnlineName}{Common.NewLine}"
                    Send(Encoding.UTF8.GetBytes($"2010 NAME {GameState.OnlineName}{Common.NewLine}"));

                    using (var m2 = new MemoryStream(128))
                    {
                        using (var w2 = new BinaryWriter(m2))
                        {
                            if (true)
                            {
                                w2.Write(System.Convert.ToUInt32(SID_JOINCHANNEL.Flags.First));
                                w2.Write(Product.ProductChannelName(GameState.Product));
                                new SID_JOINCHANNEL(m2.ToArray()).Invoke(new MessageContext(this, Protocols.MessageDirection.ClientToServer));
                            }
                        }
                    }


                    // At this point the CurrentLine could be empty again, if it is push the text forward again
                    text = text.Substring(currentPos + indexCount);
                    continue;
                }
                else
                {
                    if (!string.IsNullOrEmpty(CurrentLine))
                    {
                        using (var m3 = new MemoryStream(1 + Encoding.UTF8.GetByteCount(CurrentLine)))
                        {
                            using (var w3 = new BinaryWriter(m3))
                            {
                                if (true)
                                {
                                    w3.Write(CurrentLine);
                                    new SID_CHATCOMMAND(m3.ToArray()).Invoke(new MessageContext(this, Protocols.MessageDirection.ClientToServer));
                                }
                            }
                        }
                    }


                    // At this point the CurrentLine could be empty again, if it is push the text forward again
                    text = text.Substring(currentPos + indexCount);
                    continue;
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
