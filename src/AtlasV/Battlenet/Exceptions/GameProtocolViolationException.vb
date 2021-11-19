Imports System

Namespace AtlasV.Battlenet.Exceptions
    Class GameProtocolViolationException
        Inherits GameProtocolException

        Public Sub New(ByVal varClient As ClientState)
            MyBase.New(varClient)
        End Sub

        Public Sub New(ByVal varClient As ClientState, ByVal varMessage As String)
            MyBase.New(varClient, varMessage)
        End Sub

        Public Sub New(ByVal varClient As ClientState, ByVal varMessage As String, ByVal varInnerException As Exception)
            MyBase.New(varClient, varMessage, varInnerException)
        End Sub
    End Class
End Namespace
