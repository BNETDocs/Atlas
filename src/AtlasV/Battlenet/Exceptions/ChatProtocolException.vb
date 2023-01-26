Imports System

Namespace AtlasV.Battlenet.Exceptions
    Class ChatProtocolException
        Inherits ProtocolException

        Public Sub New(ByVal varClient As ClientState)
            MyBase.New(Battlenet.ProtocolType.Types.Chat, varClient)
        End Sub

        Public Sub New(ByVal varClient As ClientState, ByVal varMessage As String)
            MyBase.New(Battlenet.ProtocolType.Types.Chat, varClient, varMessage)
        End Sub

        Public Sub New(ByVal varClient As ClientState, ByVal varMessage As String, ByVal varInnerException As Exception)
            MyBase.New(Battlenet.ProtocolType.Types.Chat, varClient, varMessage, varInnerException)
        End Sub
    End Class
End Namespace
