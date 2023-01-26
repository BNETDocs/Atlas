Imports System
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Text.Json

Namespace AtlasV.Daemon
    Public Class Common

#Disable Warning CA2211 ' Non-constant fields should not be visible
        Public Shared HTTPListener As Battlenet.Protocols.Http.HttpListener = Nothing
        Public Shared TcpNoDelay As Boolean = False
#Enable Warning CA2211 ' Non-constant fields should not be visible

        Public Shared Sub Initialize()
            InitializeListener()
        End Sub

        Public Shared Sub InitializeListener()
            Dim httpJson, listenerJson, interfaceJson, portJson As JsonElement
            Settings.State.RootElement.TryGetProperty("http", httpJson)
            httpJson.TryGetProperty("listener", listenerJson)
            listenerJson.TryGetProperty("interface", interfaceJson)
            listenerJson.TryGetProperty("port", portJson)

            Dim listenerAddressStr = interfaceJson.GetString()
            Dim listenerAddress As New IPAddress(0)
            If IPAddress.TryParse(listenerAddressStr, listenerAddress) = False Then
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Unable to parse IP address from [http.listener.interface] with value [{listenerAddressStr}]; using any")
                listenerAddress = IPAddress.Any
            End If
            Dim listenerPort As Int32
            If portJson.TryGetInt32(listenerPort) = False Then
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Unable to parse port from [http.listener.port] with value [{portJson}]; using 8080")
                listenerPort = 8080
            End If
            Dim listenerEndPoint As New IPEndPoint(0, 0)
            If IPEndPoint.TryParse($"{listenerAddress}:{listenerPort}", listenerEndPoint) = False Then
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Unable to parse endpoint with value [{listenerAddress}:{listenerPort}]")
                Return
            End If
            HttpListener = New Battlenet.Protocols.Http.HttpListener(listenerEndPoint)
        End Sub

        Public Shared Function ReverseString(varValue As String) As String
            If varValue.Length < 2 Then
                Return varValue
            End If
            Dim charAr() As Char = varValue.ToCharArray
            Array.Reverse(charAr)
            Return New String(charAr)
        End Function

        Public Shared Function TryToInt32FromString(ByVal varValue As String,
                                                    <Out> ByRef varNumber As Integer,
                                                    ByVal Optional varDefaultNumber As Integer = 0) As Boolean
            Dim v As String = varValue

            Try

                If v.StartsWith("0b") OrElse v.StartsWith("0B") OrElse v.StartsWith("&b") OrElse v.StartsWith("&B") Then
                    varNumber = Convert.ToInt32(v.Substring(2), 2) 'v(2..)
                End If

                If v.StartsWith("-0b") OrElse v.StartsWith("-0B") OrElse v.StartsWith("-&b") OrElse v.StartsWith("-&B") Then
                    varNumber = 0 - Convert.ToInt32(v.Substring(3), 2) 'v(3..)
                ElseIf v.StartsWith("0x") OrElse v.StartsWith("0X") OrElse v.StartsWith("&h") OrElse v.StartsWith("&H") Then
                    varNumber = Convert.ToInt32(v.Substring(2), 16) 'v(2..)
                ElseIf v.StartsWith("-0x") OrElse v.StartsWith("-0X") OrElse v.StartsWith("-&h") OrElse v.StartsWith("-&H") Then
                    varNumber = Convert.ToInt32(v.Substring(3), 16) 'v(3..)
                ElseIf v.StartsWith("0") AndAlso v.Length > 1 Then
                    varNumber = Convert.ToInt32(v.Substring(1), 8) 'v(1..)
                ElseIf v.StartsWith("-0") AndAlso v.Length > 2 Then
                    varNumber = Convert.ToInt32(v.Substring(2), 8) 'v(2..)
                Else
                    varNumber = Convert.ToInt32(v, 10)
                End If

            Catch ex As Exception

                If TypeOf ex Is ArgumentException OrElse TypeOf ex Is ArgumentOutOfRangeException OrElse TypeOf ex Is FormatException OrElse TypeOf ex Is OverflowException Then
                    varNumber = varDefaultNumber
                    Return False
                Else
                    Throw
                End If
            End Try

            Return True
        End Function

        Public Shared Function TryToUInt32FromString(ByVal varValue As String,
                                                     <Out> ByRef varNumber As UInteger,
                                                     ByVal Optional varDefaultNumber As UInteger = 0) As Boolean
            Dim v As String = varValue

            Try

                If v.StartsWith("0b") OrElse v.StartsWith("0B") OrElse v.StartsWith("&b") OrElse v.StartsWith("&B") Then
                    varNumber = Convert.ToUInt32(v.Substring(2), 2)
                ElseIf v.StartsWith("0x") OrElse v.StartsWith("0X") OrElse v.StartsWith("&h") OrElse v.StartsWith("&H") Then
                    varNumber = Convert.ToUInt32(v.Substring(2), 16)
                ElseIf v.StartsWith("0") AndAlso v.Length > 1 Then
                    varNumber = Convert.ToUInt32(v.Substring(1), 8)
                Else
                    varNumber = Convert.ToUInt32(v, 10)
                End If

            Catch ex As Exception
                If TypeOf ex Is ArgumentException OrElse TypeOf ex Is ArgumentOutOfRangeException OrElse TypeOf ex Is FormatException OrElse TypeOf ex Is OverflowException Then
                    varNumber = varDefaultNumber
                    Return False
                Else
                    Throw
                End If
            End Try

            Return True
        End Function
    End Class
End Namespace
