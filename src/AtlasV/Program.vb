Imports AtlasV.Daemon
Imports System
Imports System.Diagnostics
Imports System.Threading
Imports System.Threading.Tasks

Namespace AtlasV
    Class Program
        Public Const DistributionMode As String = "release"
        Public Shared [Exit] As Boolean = False
        Public Shared ExitCode As Integer = 0
        Public Shared Property TickCountAtInit As Long = Environment.TickCount64

        Public Shared Async Function AsyncMain(ByVal args As String()) As Task(Of Integer)
            Thread.CurrentThread.Name = "Main"
            Dim assembly = GetType(Program).Assembly
            Console.WriteLine($"[{DateTime.Now}] Welcome to {assembly.GetName().Name}!")
            Console.WriteLine($"[{DateTime.Now}] Build: {assembly.GetName().Version} ({DistributionMode})")
            ParseCommandLineArgs(args)
            Settings.Initialize()
            Dim logLevel = Settings.GetString(New String() {"logging", "level"}, "Debug")
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Setting log level to {logLevel}")
            Logging.CurrentLogLevel = Logging.StringToLogLevel(logLevel)
            Common.Initialize()
            Battlenet.Common.Initialize()
            Battlenet.Common.UdpListener.Start()
            Battlenet.Common.Listener.Start()
            Common.HttpListener.Start()

            '***************************************************
            '* Search for: CHECK THIS SHIT and do what it says *
            '* - Once its running that is -                    *
            '***************************************************

            While Not [Exit]
                Await Task.Delay(1)
                Await Task.Yield()
            End While

            Return ExitCode
        End Function

        Private Shared Sub ParseCommandLineArgs(ByVal args As String())
            Dim arg As String
            Dim value As String

            For i = 0 To args.Length - 1
                arg = args(i)

                If arg.Contains("="c) Then
                    Dim p = arg.IndexOf("="c)
                    value = arg.Substring(p + 1)
                    arg = arg.Substring(0, p)
                ElseIf i + 1 < args.Length Then
                    value = args(System.Threading.Interlocked.Increment(i))
                Else
                    value = ""
                End If

                Dim r = ParseCommandLineArg(arg, value)

                If r <> 0 Then
                    Program.ExitCode = r
                    Program.[Exit] = True
                    Return
                End If
            Next
        End Sub

        Private Shared Function ParseCommandLineArg(ByVal arg As String, ByVal value As String) As Integer
            Const EXIT_SUCCESS As Integer = 0
            Const EXIT_FAILURE As Integer = 1

            Select Case arg
                Case "-c", "--config"
                    Settings.SetPath(value)
                    Exit Select
                Case Else
                    Logging.WriteLine(Logging.LogLevel.[Error], Logging.LogType.Config, $"Invalid argument [{arg}]")
                    Return EXIT_FAILURE
            End Select

            Return EXIT_SUCCESS
        End Function

    End Class
    Public Module MainApp
        Function Main(args As String()) As Integer
            Program.AsyncMain(args).Wait()
            Return 0
        End Function

    End Module
End Namespace
