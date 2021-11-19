Imports AtlasV.Battlenet
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading.Tasks

Namespace AtlasV.Battlenet.Protocols.Udp
    Class UdpListener
        Implements IDisposable, IListener

        Public ReadOnly Property IsListening As Boolean Implements IListener.IsListening
            Get
                Return Socket IsNot Nothing
            End Get
        End Property

        Public Property LocalEndPoint As IPEndPoint Implements IListener.LocalEndPoint
        Public Property Socket As Socket Implements IListener.Socket

        Public Sub New(ByVal varEndPoint As IPEndPoint)
            SetLocalEndPoint(varEndPoint)
        End Sub

        Public Sub Close() Implements IListener.Close
            [Stop]()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Close()
        End Sub

        Public Sub Parse(ByVal datagram As Byte(), ByVal remoteEndpoint As EndPoint)
            If datagram.Length < 4 Then
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received junk datagram ({datagram.Length} bytes < 4 bytes)")
                Return
            End If

            Try
                Using m = New MemoryStream(datagram)
                    Using r = New BinaryReader(m)
                        Dim messageId As UInt32 = r.ReadUInt32()

                        Select Case messageId
                            Case &H0
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received junk datagram [PKT_STORM] ({datagram.Length} bytes)")
                                Exit Select
                            Case &H3

                                If datagram.Length <> 8 Then
                                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received echo request [PKT_CLIENTREQ] ({datagram.Length} bytes != 8 bytes)")
                                Else
                                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received echo request [PKT_CLIENTREQ] ({datagram.Length} bytes); replying")
                                    Socket.SendTo(datagram, remoteEndpoint)
                                End If

                                Exit Select
                            Case &H7

                                If datagram.Length <> 8 Then
                                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received keepalive [PKT_KEEPALIVE] ({datagram.Length} bytes != 8 bytes)")
                                Else
                                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received keepalive [PKT_KEEPALIVE] ({datagram.Length} bytes)")
                                End If

                                Exit Select
                            Case &H8

                                If datagram.Length <> 8 Then
                                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received UDP test [PKT_CONNTEST] ({datagram.Length} bytes != 8 bytes)")
                                Else
                                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received UDP test [PKT_CONNTEST] ({datagram.Length} bytes)")
                                    Dim code = New Byte() {&H5, &H0, &H0, &H0, &H74, &H65, &H6E, &H62}
                                    Socket.SendTo(code, remoteEndpoint)
                                End If

                                Exit Select
                            Case &H9

                                If datagram.Length <> 12 Then
                                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received UDP test [PKT_CONNTEST2] ({datagram.Length} bytes != 12 bytes)")
                                Else
                                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received UDP test [PKT_CONNTEST2] ({datagram.Length} bytes)")
                                    Dim code = New Byte() {&H5, &H0, &H0, &H0, &H74, &H65, &H6E, &H62}
                                    Socket.SendTo(code, remoteEndpoint)
                                End If

                                Dim serverToken = r.ReadUInt32()
                                Dim udpToken = r.ReadUInt32()
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"({serverToken} - {udpToken})")

                                For Each state As ClientState In Battlenet.Common.ActiveClientStates

                                    If state.GameState.ServerToken = serverToken Then

                                        If state.GameState.UDPToken = udpToken Then
                                            Dim ipEndPoint = TryCast(remoteEndpoint, IPEndPoint)

                                            If ipEndPoint IsNot Nothing Then
                                                state.GameState.GameDataAddress = ipEndPoint.Address
                                                state.GameState.GameDataPort = CUShort(ipEndPoint.Port)
                                            End If
                                        End If

                                        Exit For
                                    End If
                                Next

                                Exit Select
                            Case Else
                                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, remoteEndpoint, $"Received junk datagram ({datagram.Length} bytes)")
                                Exit Select
                        End Select

                    End Using
                End Using

            Catch e As Exception
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_UDP, remoteEndpoint, $"{e.[GetType]().Name} error occurred while parsing UDP datagram")
            End Try
        End Sub

        Private Sub ReceiveFromAsyncCompleted(ByVal sender As Object, ByVal varSocketAsyncEventArgs As SocketAsyncEventArgs)
            If varSocketAsyncEventArgs.SocketError <> SocketError.Success Then
                Logging.WriteLine(Logging.LogLevel.[Error], Logging.LogType.Client_UDP, varSocketAsyncEventArgs.RemoteEndPoint, $"Socket error occurred. Stopping UDP service.")
                [Stop]()
                Return
            End If

            If varSocketAsyncEventArgs.LastOperation <> SocketAsyncOperation.ReceiveFrom Then Return
            'CHECK THIS SHIT
            'Dim bytes() As Byte = e.Buffer(e.Offset..e.BytesTransferred)
            Dim bytes() As Byte = varSocketAsyncEventArgs.Buffer.Skip(varSocketAsyncEventArgs.Offset).Take(varSocketAsyncEventArgs.BytesTransferred).ToArray()
            Dim endp = varSocketAsyncEventArgs.RemoteEndPoint
            Dim buf = String.Empty
            Dim pre = String.Empty

            For i = 0 To bytes.Length - 1

                If i Mod 16 = 0 AndAlso Not String.IsNullOrEmpty(pre) Then
                    buf += pre & Environment.NewLine
                    pre = String.Empty
                End If

                buf += $"{bytes(i)} "
                pre += If(i > 32 AndAlso i < 127, Convert.ToChar(bytes(i)), "."c)
            Next

            buf += pre
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_UDP, varSocketAsyncEventArgs.RemoteEndPoint, $"Received Datagram: {buf}")
            Parse(bytes, endp)
            varSocketAsyncEventArgs.RemoteEndPoint = New IPEndPoint(IPAddress.Any, 0)
            varSocketAsyncEventArgs.SetBuffer(New Byte(2047) {}, 0, 2048)
            Dim willRaiseEvent As Boolean = Socket.ReceiveFromAsync(varSocketAsyncEventArgs)

            If Not willRaiseEvent Then
                ReceiveFromAsyncCompleted(Me, varSocketAsyncEventArgs)
            End If
        End Sub

        Public Sub SetLocalEndPoint(ByVal varEndPoint As IPEndPoint) Implements IListener.SetLocalEndPoint
            If IsListening Then
                Throw New InvalidOperationException()
            End If

            LocalEndPoint = varEndPoint
        End Sub

        Public Sub Start() Implements IListener.Start
            [Stop]()
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Starting UDP listener on [{LocalEndPoint}]")
            Socket = New Socket(SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp)
            Socket.Bind(LocalEndPoint)
            Dim e = New SocketAsyncEventArgs()
            AddHandler e.Completed, New EventHandler(Of SocketAsyncEventArgs)(AddressOf ReceiveFromAsyncCompleted)
            e.RemoteEndPoint = New IPEndPoint(IPAddress.Any, 0)
            e.SetBuffer(New Byte(2047) {}, 0, 2048)
            Dim willRaiseEvent As Boolean = Socket.ReceiveFromAsync(e)

            If Not willRaiseEvent Then
                ReceiveFromAsyncCompleted(Me, e)
            End If
        End Sub

        Public Sub [Stop]() Implements IListener.Stop
            If Not IsListening Then Return
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Stopping UDP listener on [{Socket.LocalEndPoint}]")
            Socket.Close()
            Socket = Nothing
        End Sub

    End Class
End Namespace
