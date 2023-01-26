Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Battlenet.Protocols.BNFTP
Imports AtlasV.Battlenet.Protocols.Game
Imports AtlasV.Battlenet.Protocols.Game.Messages
Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System
Imports System.IO
Imports System.Linq
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading.Tasks

Namespace AtlasV.Battlenet
    Class ClientState
        Implements IDisposable

        Public BNFTPState As BNFTPState

        Public ReadOnly Property Connected As Boolean
            Get
                Return Socket IsNot Nothing AndAlso Socket.Connected
            End Get
        End Property

        Public Property IsDisposing As Boolean = False
        Public Property GameState As GameState
        Public Property ProtocolType As ProtocolType
        Public Property RemoteEndPoint As System.Net.EndPoint
        Public Property Socket As Socket
        Protected ReceiveBuffer As Byte() = Array.Empty(Of Byte)()
        Protected SendBuffer As Byte() = Array.Empty(Of Byte)()
        Protected BattlenetGameFrame As New Frame()

        Public Sub New(ByVal client As Socket)
            Initialize(client)
        End Sub

        Public Sub Close()
            Disconnect()
        End Sub

        Public Sub Disconnect(ByVal Optional reason As String = Nothing)
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, RemoteEndPoint, "TCP connection forcefully closed by server")

            If reason IsNot Nothing Then

                If GameState IsNot Nothing Then
                    Dim r = If(reason.Length = 0, Resources.DisconnectedByAdmin, Resources.DisconnectedByAdminWithReason)
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, GameState.ChannelFlags, GameState.Ping, GameState.OnlineName, r).WriteTo(Me)
                End If
            End If

            Try

                If GameState IsNot Nothing Then
                    GameState.Close()
                End If

            Catch __unusedObjectDisposedException1__ As ObjectDisposedException
            Finally
                GameState = Nothing
            End Try

            SyncLock Common.ActiveClientStates
                Common.ActiveClientStates.Remove(Me)
            End SyncLock

            Try
                Socket.Shutdown(SocketShutdown.Send)
            Catch ex As Exception
                If Not (TypeOf ex Is SocketException OrElse TypeOf ex Is ObjectDisposedException) Then Throw
            Finally

                If Socket IsNot Nothing Then
                    Socket.Close()
                End If
            End Try
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If IsDisposing Then Return
            IsDisposing = True
            Disconnect()
            IsDisposing = False
        End Sub

        Protected Sub Initialize(ByVal client As Socket)
            SyncLock Common.ActiveClientStates
                Common.ActiveClientStates.Add(Me)
            End SyncLock

            BNFTPState = Nothing
            GameState = Nothing
            ProtocolType = Nothing
            RemoteEndPoint = client.RemoteEndPoint
            Socket = client
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, "TCP connection established")
            client.NoDelay = Daemon.Common.TcpNoDelay
            client.ReceiveTimeout = 500
            client.SendTimeout = 500

            If client.ReceiveBufferSize < &HFFFF Then
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client, RemoteEndPoint, "Setting ReceiveBufferSize to [0xFFFF]")
                client.ReceiveBufferSize = &HFFFF
            End If

            If client.SendBufferSize < &HFFFF Then
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client, RemoteEndPoint, "Setting SendBufferSize to [0xFFFF]")
                client.SendBufferSize = &HFFFF
            End If
        End Sub

        Private Sub Invoke(ByVal e As SocketAsyncEventArgs)
            If e.SocketError <> SocketError.Success Then Return
            Dim context = New MessageContext(Me, Protocols.MessageDirection.ClientToServer)
            Dim msg = Nothing

            SyncLock BattlenetGameFrame.Messages

                While Not BattlenetGameFrame.Messages.IsEmpty '.Count > 0

                    If Not BattlenetGameFrame.Messages.TryDequeue(msg) Then
                        Disconnect()
                        Exit While
                    End If

                    If Not msg.Invoke(context) Then
                        Disconnect()
                        Exit While
                    End If
                End While
            End SyncLock
        End Sub

        Public Sub ProcessReceive(ByVal e As SocketAsyncEventArgs)
            If Not (e.SocketError = SocketError.Success AndAlso e.BytesTransferred > 0) Then

                If Not IsDisposing AndAlso Socket IsNot Nothing Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"TCP connection lost")
                    Dispose()
                End If

                Return
            End If

            SyncLock ReceiveBuffer
                Dim newBuffer = New Byte(ReceiveBuffer.Length + e.BytesTransferred - 1) {}
                Buffer.BlockCopy(ReceiveBuffer, 0, newBuffer, 0, ReceiveBuffer.Length)
                Buffer.BlockCopy(e.Buffer, e.Offset, newBuffer, ReceiveBuffer.Length, e.BytesTransferred)
                ReceiveBuffer = newBuffer
            End SyncLock

            If ProtocolType Is Nothing Then ReceiveProtocolType(e)
            ReceiveProtocol(e)
        End Sub

        Public Sub ProcessSend(ByVal e As SocketAsyncEventArgs)
            If e.SocketError <> SocketError.Success Then

                If Not IsDisposing AndAlso Socket IsNot Nothing Then
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"TCP connection lost")
                    Dispose()
                End If

                Return
            End If
        End Sub

        Public Sub ReceiveAsync()
            If Socket Is Nothing Then Return
            Dim readEventArgs = New SocketAsyncEventArgs()
            AddHandler readEventArgs.Completed, New EventHandler(Of SocketAsyncEventArgs)(AddressOf SocketIOCompleted)
            readEventArgs.SetBuffer(New Byte(1023) {}, 0, 1024)
            readEventArgs.UserToken = Me
            Dim willRaiseEvent As Boolean

            Try
                willRaiseEvent = Socket.ReceiveAsync(readEventArgs)
            Catch __unusedObjectDisposedException1__ As ObjectDisposedException
                Return
            End Try

            If Not willRaiseEvent Then
                SocketIOCompleted(Me, readEventArgs)
            End If
        End Sub

        Protected Sub ReceiveProtocolType(varSckArgs As SocketAsyncEventArgs)
            If ProtocolType IsNot Nothing Then Return
            ProtocolType = New ProtocolType(CType(ReceiveBuffer(0), ProtocolType.Types))
            ReceiveBuffer = ReceiveBuffer.Skip(1).ToArray() '(1..)

            If ProtocolType.IsGame() OrElse ProtocolType.IsChat() Then
                GameState = New GameState(Me)
            ElseIf ProtocolType.IsBNFTP() Then
                BNFTPState = New BNFTPState(Me)
            End If

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client, RemoteEndPoint, $"Set protocol type [0x{CByte(ProtocolType.Type)}] ({ProtocolType})")

            If ProtocolType.IsChat() Then
                GameState.Platform = Platform.PlatformCode.Windows
                GameState.Product = Product.ProductCode.Chat
            End If
        End Sub

        Protected Sub ReceiveProtocol(varSckArgs As SocketAsyncEventArgs)
            If varSckArgs.SocketError <> SocketError.Success Then Return

            Select Case ProtocolType.Type
                Case ProtocolType.Types.Game
                    ReceiveProtocolGame(varSckArgs)
                Case ProtocolType.Types.BNFTP
                    ReceiveProtocolBNFTP(varSckArgs)
                Case ProtocolType.Types.Chat, ProtocolType.Types.Chat_Alt1, ProtocolType.Types.Chat_Alt2
                    ReceiveProtocolChat(varSckArgs)
                Case Else
                    Throw New ProtocolNotSupportedException(ProtocolType.Type, Me, $"Unsupported protocol type [0x{CByte(ProtocolType.Type)}]")
            End Select
        End Sub

#Disable Warning IDE0060 ' Remove unused parameter
        Protected Sub ReceiveProtocolBNFTP(varSckArgs As SocketAsyncEventArgs)
#Enable Warning IDE0060 ' Remove unused parameter
            If ReceiveBuffer.Length = 0 Then Return
            BNFTPState.Receive(ReceiveBuffer)
        End Sub

#Disable Warning IDE0060 ' Remove unused parameter
        Protected Sub ReceiveProtocolChat(varSckArgs As SocketAsyncEventArgs)
#Enable Warning IDE0060 ' Remove unused parameter
            Dim text As String

            Try
                text = Encoding.UTF8.GetString(ReceiveBuffer)
            Catch __unusedDecoderFallbackException1__ As DecoderFallbackException
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Chat, RemoteEndPoint, "Failed to decode UTF-8 text")
                Disconnect("Failed to decode UTF-8 text")
                Return
            End Try

            'Fill the lineBuffer
            Dim indexCr, indexLf, indexCount, currentPos As Integer
            Dim CurrentLine As String
            While text.Contains(Convert.ToChar(13)) OrElse text.Contains(Convert.ToChar(10))
                'Topaz uses carriagereturn's (cr x3 x4 name cr pass cr [data cr])
                'UltimateBot (crlf, x3 x4 crlf name crlf pass crlf [data crlf])
                'MiniChat (x3 x4 crlf name crlf pass crlf [data crlf])
                '
                '   there was a few that used a similar data set to topaz where it wasent cr's but was lf's
                '   and there is likely one's that used a mix of both.
                '   pretty sure there was a login in the form of (x3 name crlf x4 pass crlf) aswell
                '

#Region " Gather the index location and the length of the delimiter "
                'need to check for mixing
                indexCr = text.IndexOf(Convert.ToChar(13)) 'since vbLf is busted vbCr probably isnt far behind that.
                indexLf = text.IndexOf(Convert.ToChar(10)) 'apparently vbLf is busted, but thats ok

                If indexCr > -1 AndAlso indexLf > -1 Then
                    'Are they side by side
                    If indexCr < indexLf Then
                        If (indexLf - indexCr) = 1 Then
                            'theyre side by side
                            indexCount = 2
                        Else
                            'theyre not side by side
                            indexCount = 1
                        End If
                        currentPos = indexCr
                    Else 'indexCr > indexLf
                        If (indexCr - indexLf) = 1 Then
                            'theyre side by side
                            indexCount = 2
                        Else
                            'theyre not side by side
                            indexCount = 1
                        End If
                        currentPos = indexLf
                    End If
                Else
                    'There was only 1 instance of either carriagereturn or linefeed
                    If indexCr > -1 Then
                        currentPos = indexCr
                    Else 'If indexLf > -1 Then [no need to test again, 1 or the other is here]
                        currentPos = indexLf
                    End If
                    indexCount = 1
                End If

                ' currentPos = location of either the cr or the lf or both if side by side
                ' indexCount = length of the delimiter if the cr and the lf are side by side then its 2 otherwise its 1
#End Region
                ' Text holds the pre-processed UTF8 ReceiveBuffer
                ' ReceiveBuffer is moved to the next line
                ReceiveBuffer = ReceiveBuffer.Skip(currentPos + indexCount).ToArray()
                ' CurrentLine is the first line 
                CurrentLine = text.Substring(0, currentPos)

                ' was the line empty, and is there still data in the text buf
                If CurrentLine = "" Then
                    'move the text forward and push us back to the top of the while loop
                    text = text.Substring(currentPos + indexCount)
                    If text = "" Then
                        'This was required for UB 4.13
                        Send(Encoding.UTF8.GetBytes($"Enter your Account name: {Common.NewLine}"))
                    End If
                    Continue While
                End If

                ' if GameState.ActiveAccount is nothing then the login process hasent completed yet
                If GameState.ActiveAccount Is Nothing Then
#Region " Telnet Chat Initial Data Flow "
                    ' text holds the current line of data first order of buisness we are looking for
                    ' 0x3 and 0x4, since the login could have come from Topaz or UltimateBot
                    If CurrentLine(0) = Convert.ToChar(&H3) Then
                        CurrentLine = CurrentLine.Substring(1)
                    End If
                    ' Because the line may actually be empty check the length aswell, we will get this
                    ' on the next pass. the above we know for sure is not empty.
                    If CurrentLine.Length >= 1 AndAlso CurrentLine(0) = Convert.ToChar(&H4) Then
                        CurrentLine = CurrentLine.Substring(1)
                        ' 
                        Send(Encoding.UTF8.GetBytes($"Enter your Account name: {Common.NewLine}"))
                    End If
                    ' At this point the CurrentLine could be empty again, if it is push the text forward again
                    If CurrentLine = "" Then 'we had x3 crlf or we had x4 crlf
                        'move the text forward and push us back to the top of the while loop
                        text = text.Substring(currentPos + indexCount)
                        Continue While
                    End If

                    ' Second order of work we're looking for the UserName
                    If String.IsNullOrEmpty(GameState.Username) Then
                        Send(Encoding.UTF8.GetBytes($"Username: {CurrentLine}{Common.NewLine}"))
                        GameState.Username = CurrentLine
                        Send(Encoding.UTF8.GetBytes($"Enter your Account pass: {Common.NewLine}"))
                        text = text.Substring(currentPos + indexCount)
                        Continue While
                    End If

                    ' Third we are looking for the Password to process the account forward
                    Dim varAccount As Account = Nothing
                    Dim inPasswordHash = MBNCSUtil.XSha1.CalculateHash(Encoding.UTF8.GetBytes(CurrentLine))
                    Common.AccountsDb.TryGetValue(GameState.Username, varAccount)

                    If varAccount Is Nothing Then
                        GameState.Username = Nothing
                        Send(Encoding.UTF8.GetBytes($"Incorrect username/password.{Common.NewLine}"))
                        Return
                    End If

                    Dim dbPasswordHash = CType(varAccount.[Get](Account.PasswordKey, New Byte(19) {},), Byte())

                    If Not inPasswordHash.SequenceEqual(dbPasswordHash) Then
                        GameState.Username = Nothing
                        Send(Encoding.UTF8.GetBytes($"Incorrect username/password.{Common.NewLine}"))
                        Return
                    End If

                    Dim flags = CType(varAccount.[Get](Account.FlagsKey, Account.Flags.None,), Account.Flags)

                    If (flags And Account.Flags.Closed) <> 0 Then
                        GameState.Username = Nothing
                        Send(Encoding.UTF8.GetBytes($"Account closed.{Common.NewLine}"))
                        Return
                    End If

                    GameState.ActiveAccount = varAccount
                    GameState.LastLogon = CDate(varAccount.[Get](Account.LastLogonKey, DateTime.Now,))
                    varAccount.[Set](Account.IPAddressKey, RemoteEndPoint.ToString().Split(":")(0))
                    varAccount.[Set](Account.LastLogonKey, DateTime.Now)
                    varAccount.[Set](Account.PortKey, RemoteEndPoint.ToString().Split(":")(1))

                    SyncLock Common.ActiveAccounts
                        Dim locSerial = 1
                        Dim locOnlineName = GameState.Username

                        While Common.ActiveAccounts.ContainsKey(locOnlineName.ToLower())
                            locOnlineName = $"{GameState.Username}#{System.Threading.Interlocked.Increment(locSerial)}"
                        End While

                        GameState.OnlineName = locOnlineName
                        Common.ActiveAccounts.Add(locOnlineName.ToLower(), varAccount)
                    End SyncLock

                    GameState.Username = CStr(varAccount.[Get](Account.UsernameKey, GameState.Username))

                    SyncLock Common.ActiveGameStates
                        Common.ActiveGameStates.Add(GameState.OnlineName.ToLower(), GameState)
                    End SyncLock

                    Send(Encoding.UTF8.GetBytes($"Connection from [{RemoteEndPoint}]{Common.NewLine}"))
                    GameState.GenerateStatstring()

                    ' [Telnet SID_ENTERCHAT] = "2010 NAME {GameState.OnlineName}{Common.NewLine}"
                    Send(Encoding.UTF8.GetBytes($"2010 NAME {GameState.OnlineName}{Common.NewLine}"))

                    Using m2 = New MemoryStream(128)
                        Using w2 = New BinaryWriter(m2)
                            If True Then
                                w2.Write(CUInt(SID_JOINCHANNEL.Flags.First))
                                w2.Write(Product.ProductChannelName(GameState.Product))
                                Call New SID_JOINCHANNEL(m2.ToArray()).Invoke(New MessageContext(Me, Protocols.MessageDirection.ClientToServer))
                            End If
                        End Using
                    End Using


                    ' At this point the CurrentLine could be empty again, if it is push the text forward again
                    text = text.Substring(currentPos + indexCount)
                    Continue While

#End Region
                Else 'login has completed they now have the ability to send regular data
#Region " Telnet Live Messaging "
                    If Not String.IsNullOrEmpty(CurrentLine) Then
                        Using m3 = New MemoryStream(1 + Encoding.UTF8.GetByteCount(CurrentLine))
                            Using w3 = New BinaryWriter(m3)
                                If True Then
                                    w3.Write(CurrentLine)
                                    Call New SID_CHATCOMMAND(m3.ToArray()).Invoke(New MessageContext(Me, Protocols.MessageDirection.ClientToServer))
                                End If
                            End Using
                        End Using
                    End If


                    ' At this point the CurrentLine could be empty again, if it is push the text forward again
                    text = text.Substring(currentPos + indexCount)
                    Continue While
#End Region
                End If

            End While
        End Sub

        Protected Sub ReceiveProtocolGame(ByVal e As SocketAsyncEventArgs)
            Dim newBuffer As Byte()

            While ReceiveBuffer.Length > 0
                If ReceiveBuffer.Length < 4 Then Return
                Dim messageLen As UInt16 = ReceiveBuffer(3) : messageLen <<= 8 : messageLen += ReceiveBuffer(2)
                If ReceiveBuffer.Length < messageLen Then Return
                Dim messageId As Byte = ReceiveBuffer(1)
                Dim messageBuffer As Byte() = New Byte(messageLen - 4 - 1) {}
                Buffer.BlockCopy(ReceiveBuffer, 4, messageBuffer, 0, messageLen - 4)
                newBuffer = New Byte(ReceiveBuffer.Length - messageLen - 1) {}
                Buffer.BlockCopy(ReceiveBuffer, messageLen, newBuffer, 0, ReceiveBuffer.Length - messageLen)
                ReceiveBuffer = newBuffer
                Dim message As Message = message.FromByteArray(messageId, messageBuffer)

                If TypeOf message Is Message Then
                    BattlenetGameFrame.Messages.Enqueue(message)
                    Continue While
                Else
                    Throw New GameProtocolException(Me, $"Received unknown SID_0x{messageId} ({messageLen} bytes)")
                End If
            End While

            Invoke(e)
        End Sub

        Public Sub Send(ByVal buffer As Byte())
            If Socket Is Nothing Then Return
            Dim e = New SocketAsyncEventArgs()
            AddHandler e.Completed, New EventHandler(Of SocketAsyncEventArgs)(AddressOf SocketIOCompleted)
            e.SetBuffer(buffer, 0, buffer.Length)
            e.UserToken = Me
            Dim willRaiseEvent As Boolean

            Try
                willRaiseEvent = Socket.SendAsync(e)
            Catch __unusedObjectDisposedException1__ As ObjectDisposedException
                Return
            End Try

            If Not willRaiseEvent Then
                SocketIOCompleted(Me, e)
            End If
        End Sub

        Private Sub SocketIOCompleted(ByVal sender As Object, ByVal e As SocketAsyncEventArgs)
            Dim clientState = TryCast(e.UserToken, ClientState)

            Try

                Select Case e.LastOperation
                    Case SocketAsyncOperation.Receive
                        clientState.ProcessReceive(e)
                    Case SocketAsyncOperation.Send
                        clientState.ProcessSend(e)
                    Case Else
                        Throw New ArgumentException("The last operation completed on the socket was not a receive or send")
                End Select

            Catch ex As GameProtocolViolationException
                Logging.WriteLine(Logging.LogLevel.Warning, CType(ProtocolType.ProtocolTypeToLogType(ex.ProtocolType), Logging.LogType), clientState.RemoteEndPoint, "Protocol violation encountered!" & (If(ex.Message.Length > 0, $" {ex.Message}", "")))
                clientState.Dispose()
            Catch ex As Exception
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, clientState.RemoteEndPoint, $"{ex.[GetType]().Name} error encountered!" & (If(ex.Message.Length > 0, $" {ex.Message}", "")))
                clientState.Dispose()
            Finally

                If e.LastOperation = SocketAsyncOperation.Receive Then
                    Task.Run(Sub()
                                 ReceiveAsync()
                             End Sub)
                End If
            End Try
        End Sub

        Public Sub SocketIOCompleted_External(ByVal sender As Object, ByVal e As SocketAsyncEventArgs)
            Dim clientState = TryCast(e.UserToken, ClientState)

            If Object.Equals(clientState, Me) = False Then
                Throw New NotSupportedException()
            End If

            SocketIOCompleted(sender, e)
        End Sub
    End Class
End Namespace
