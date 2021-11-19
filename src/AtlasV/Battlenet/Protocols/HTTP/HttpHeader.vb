Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Http
    Class HttpHeader
        Public Const MaxKeyLength As Integer = 4096
        Public Const MaxValueLength As Integer = 4096
        Protected Key As String
        Protected Value As String

        Public Sub New(ByVal varKey As String, ByVal varValue As String)
            SetKey(varKey)
            SetValue(varValue)
        End Sub

        Public Function GetKey() As String
            Return Key
        End Function

        Public Function GetValue() As String
            Return Value
        End Function

        Public Sub SetKey(ByVal varKey As String)
            If String.IsNullOrEmpty(varKey) OrElse varKey.Length > MaxKeyLength Then
                Throw New ArgumentOutOfRangeException($"value length must be between 1-{MaxKeyLength}")
            End If

            Key = varKey
        End Sub

        Public Sub SetValue(ByVal varValue As String)
            If String.IsNullOrEmpty(varValue) OrElse varValue.Length > MaxValueLength Then
                Throw New ArgumentOutOfRangeException($"value length must be between 1-{MaxValueLength}")
            End If

            Value = varValue
        End Sub

        Public Overrides Function ToString() As String
            Return $"{Key}: {Value}\r\n"
        End Function

    End Class
End Namespace
