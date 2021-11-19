Imports System

Namespace AtlasV.Battlenet.Exceptions
    Class BNFTPProtocolException
        Inherits ProtocolException

        Public Sub New(ByVal varClient As ClientState)
            MyBase.New(Battlenet.ProtocolType.Types.BNFTP, varClient)
        End Sub

        Public Sub New(ByVal varClient As ClientState, ByVal varMessage As String)
            MyBase.New(Battlenet.ProtocolType.Types.BNFTP, varClient, varMessage)
        End Sub

        Public Sub New(ByVal varClient As ClientState, ByVal varMessage As String, ByVal varInnerException As Exception)
            MyBase.New(Battlenet.ProtocolType.Types.BNFTP, varClient, varMessage, varInnerException)
        End Sub
    End Class
End Namespace
