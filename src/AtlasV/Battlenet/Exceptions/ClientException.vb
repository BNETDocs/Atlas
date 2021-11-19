Imports System
Imports System.Net

Namespace AtlasV.Battlenet.Exceptions
    Class ClientException
        Inherits Exception

        Public Property Client As ClientState

        Public Sub New(ByVal varClient As ClientState)
            MyBase.New()
            Client = varClient
        End Sub

        Public Sub New(ByVal varClient As ClientState, ByVal varMessage As String)
            MyBase.New(varMessage)
            Client = varClient
        End Sub

        Public Sub New(ByVal varClient As ClientState, ByVal varMessage As String, ByVal varInnerException As Exception)
            MyBase.New(varMessage, varInnerException)
            Client = varClient
        End Sub
    End Class
End Namespace
