Imports AtlasV.Daemon
Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading.Tasks

Namespace AtlasV.Battlenet
    Class ServerSocket
        Implements IDisposable

        Private IsDisposing As Boolean = False
        Public Property IsListening As Boolean
        Public Property LocalEndPoint As IPEndPoint
        Public Property Socket As Socket

        Public Sub New()
            IsListening = False
            LocalEndPoint = Nothing
            Socket = Nothing
        End Sub

        Public Sub New(ByVal varLocalEndPoint As IPEndPoint)
            IsListening = False
            Socket = Nothing
            SetLocalEndPoint(varLocalEndPoint)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If IsDisposing Then Return
            IsDisposing = True

            If IsListening Then
                [Stop]()
            End If

            If Socket IsNot Nothing Then
                Socket = Nothing
            End If

            If LocalEndPoint IsNot Nothing Then
                LocalEndPoint = Nothing
            End If

            IsDisposing = False
        End Sub

        Private Sub ProcessAccept(ByVal e As SocketAsyncEventArgs)
            Dim clientState = New ClientState(e.AcceptSocket)
            Task.Run(Sub()
                         clientState.ReceiveAsync()
                     End Sub)
            StartAccept(e)
        End Sub

        Public Sub SetLocalEndPoint(ByVal varLocalEndPoint As IPEndPoint)
            If IsListening Then
                Throw New NotSupportedException("Cannot set LocalEndPoint while socket is listening")
            End If

            LocalEndPoint = varLocalEndPoint

            If Socket IsNot Nothing Then
                Socket.Close()
            End If

            Socket = New Socket(LocalEndPoint.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp) With {
                .ExclusiveAddressUse = True,
                .NoDelay = Daemon.Common.TcpNoDelay,
                .UseOnlyOverlappedIO = True
            }
        End Sub

        Public Sub Start(ByVal Optional backlog As Integer = 100)
            If IsListening Then
                [Stop]()
            End If

            If LocalEndPoint Is Nothing Then
                Throw New NullReferenceException("LocalEndPoint must be set to an instance of IPEndPoint")
            End If

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Starting TCP listener on [{LocalEndPoint}]")
            Socket.Bind(LocalEndPoint)
            Socket.Listen(backlog)
            IsListening = True
            StartAccept(Nothing)
        End Sub

        Private Sub StartAccept(ByVal acceptEventArg As SocketAsyncEventArgs)
            If acceptEventArg Is Nothing Then
                acceptEventArg = New SocketAsyncEventArgs()
                AddHandler acceptEventArg.Completed, New EventHandler(Of SocketAsyncEventArgs)(AddressOf AcceptEventArg_Completed)
            Else
                acceptEventArg.AcceptSocket = Nothing
            End If

            Dim willRaiseEvent As Boolean = Socket.AcceptAsync(acceptEventArg)

            If Not willRaiseEvent Then
                ProcessAccept(acceptEventArg)
            End If
        End Sub

        Private Sub AcceptEventArg_Completed(ByVal sender As Object, ByVal e As SocketAsyncEventArgs)
            ProcessAccept(e)
        End Sub

        Public Sub [Stop]()
            If Not IsListening Then Return
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Stopping TCP listener on [{Socket.LocalEndPoint}]")
            Socket.Close()
            Socket = Nothing
            IsListening = False
        End Sub
    End Class
End Namespace