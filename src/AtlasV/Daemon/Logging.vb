Imports System
Imports System.Net

Namespace AtlasV.Daemon
    Public Class Logging

#Disable Warning CA2211 ' Non-constant fields should not be visible
        Public Shared CurrentLogLevel As LogLevel = LogLevel.Debug
#Enable Warning CA2211 ' Non-constant fields should not be visible

        Public Enum LogLevel As UInteger
            [Error]
            [Warning]
            [Info]
            [Debug]
        End Enum

        Public Enum LogType As UInteger
            Account
            BNFTP
            Channel
            Clan
            Config
            Client
            Client_BNFTP
            Client_Chat
            Client_Game
            Client_IPC
            Client_MCP
            Client_UDP
            Http
            Server
        End Enum

        Public Shared Function LogLevelToString(varValue As LogLevel) As String
            Select Case varValue
                Case LogLevel.Error : Return "Error"
                Case LogLevel.Warning : Return "Warning"
                Case LogLevel.Info : Return "Info"
                Case LogLevel.Debug : Return "Debug"
                Case Else : Throw New IndexOutOfRangeException("Unknown log level")
            End Select
            Return ""
        End Function

        Public Shared Function LogTypeToString(varValue As LogType) As String
            Select Case varValue
                Case LogType.Account : Return "Account"
                Case LogType.BNFTP : Return "BNFTP"
                Case LogType.Channel : Return "Channel"
                Case LogType.Config : Return "Config"
                Case LogType.Client : Return "Client"
                Case LogType.Client_BNFTP : Return "Client_BNFTP"
                Case LogType.Client_Chat : Return "Client_Chat"
                Case LogType.Client_Game : Return "Client_Game"
                Case LogType.Client_IPC : Return "Client_IPC"
                Case LogType.Client_MCP : Return "Client_MCP"
                Case LogType.Client_UDP : Return "Client_UDP"
                Case LogType.Http : Return "Http"
                Case LogType.Server : Return "Server"
                Case Else : Throw New IndexOutOfRangeException("Unknown log type")
            End Select
            Return ""
        End Function

        Public Shared Function StringToLogLevel(varValue As String) As LogLevel
            Select Case varValue.ToLower()
                Case "error" : Return LogLevel.Error
                Case "warning" : Return LogLevel.Warning
                Case "info" : Return LogLevel.Info
                Case "debug" : Return LogLevel.Debug
                Case Else : Throw New IndexOutOfRangeException("Unknown log level")
            End Select
            Return LogLevel.Error
        End Function

        Public Shared Sub WriteLine(varLevel As LogLevel, varType As LogType, varBuffer As String)
            If (varLevel > CurrentLogLevel) Then Return
            Console.Out.WriteLine($"[{DateTime.Now}] [{LogLevelToString(varLevel)}] [{LogTypeToString(varType).Replace("_", "] [")}] {varBuffer}")
        End Sub

        Public Shared Sub WriteLine(varLevel As LogLevel, varType As LogType, varEndPoint As EndPoint, varBuffer As String)
            WriteLine(varLevel, varType, $"[{varEndPoint}] {varBuffer}")
        End Sub

    End Class
End Namespace
