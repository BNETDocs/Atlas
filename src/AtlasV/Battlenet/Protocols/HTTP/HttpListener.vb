Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Namespace AtlasV.Battlenet.Protocols.Http
    Public Class HttpListener
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

        Private Sub ProcessAccept(ByVal varSocketAsyncEventArgs As SocketAsyncEventArgs)
            If varSocketAsyncEventArgs.SocketError <> SocketError.Success Then
                Logging.WriteLine(Logging.LogLevel.[Error], Logging.LogType.Http, varSocketAsyncEventArgs.RemoteEndPoint, "HTTP listener socket error occurred!")
            End If

            Task.Run(Sub()
                         Dim tmpSession As New HttpSession(varSocketAsyncEventArgs.AcceptSocket)
                         tmpSession.ConnectedEvent()
                     End Sub).Wait()
            'Task.Run(New HttpSession(varSocketAsyncEventArgs.AcceptSocket).ConnectedEvent()) 'how does this even work on c#, this line makes no sense.
            StartAccept(varSocketAsyncEventArgs)
        End Sub

        Public Sub SetLocalEndPoint(ByVal varEndPoint As IPEndPoint) Implements IListener.SetLocalEndPoint
            If IsListening Then
                Throw New InvalidOperationException()
            End If

            LocalEndPoint = varEndPoint
        End Sub

        Public Sub Start() Implements IListener.Start
            [Stop]()
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Starting HTTP listener on [{LocalEndPoint}]")
            Socket = New Socket(LocalEndPoint.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp) With {
                .ExclusiveAddressUse = True,
                .NoDelay = Daemon.Common.TcpNoDelay,
                .UseOnlyOverlappedIO = True
            }
            Socket.Bind(LocalEndPoint)
            Socket.Listen(-1)
            StartAccept(Nothing)
        End Sub

        Private Sub StartAccept(ByVal varAcceptEventArg As SocketAsyncEventArgs)
            If varAcceptEventArg Is Nothing Then
                varAcceptEventArg = New SocketAsyncEventArgs()
                AddHandler varAcceptEventArg.Completed, New EventHandler(Of SocketAsyncEventArgs)(AddressOf AcceptEventArg_Completed)
            Else
                varAcceptEventArg.AcceptSocket = Nothing
            End If

            Dim willRaiseEvent As Boolean = Socket.AcceptAsync(varAcceptEventArg)

            If Not willRaiseEvent Then
                ProcessAccept(varAcceptEventArg)
            End If
        End Sub

        Private Sub AcceptEventArg_Completed(ByVal sender As Object, ByVal varSocketAsyncEventArgs As SocketAsyncEventArgs)
            ProcessAccept(varSocketAsyncEventArgs)
        End Sub

        Public Sub [Stop]() Implements IListener.Stop
            If Not IsListening Then Return
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Stopping HTTP listener on [{Socket.LocalEndPoint}]")
            Socket.Close()
            Socket = Nothing
        End Sub

    End Class
End Namespace