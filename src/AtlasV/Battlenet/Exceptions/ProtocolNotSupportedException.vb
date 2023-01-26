Imports System

Namespace AtlasV.Battlenet.Exceptions
    Class ProtocolNotSupportedException
        Inherits ClientException

        Public Property ProtocolType As ProtocolType.Types

        Public Sub New(ByVal varProtocolType As ProtocolType.Types, ByVal varClient As ClientState, ByVal Optional varMessage As String = "Unsupported protocol")
            MyBase.New(varClient, varMessage)
            ProtocolType = varProtocolType
        End Sub

        Public Sub New(ByVal varProtocolType As ProtocolType.Types, ByVal varClient As ClientState, ByVal varMessage As String, ByVal varInnerException As Exception)
            MyBase.New(varClient, varMessage, varInnerException)
            ProtocolType = varProtocolType
        End Sub
    End Class
End Namespace
